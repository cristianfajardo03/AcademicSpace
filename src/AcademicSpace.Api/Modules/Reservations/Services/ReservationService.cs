using System.Data;
using AcademicSpace.Api.Modules.Reservations.Contracts;
using AcademicSpace.Api.Modules.Reservations.Entities;
using AcademicSpace.Api.Modules.Reservations.Repositories;
using AcademicSpace.Api.Modules.Spaces.Repositories;
using AcademicSpace.Api.Shared.Auth;
using AcademicSpace.Api.Shared.Errors;
using AcademicSpace.Api.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSpace.Api.Modules.Reservations.Services;

public class ReservationService(
    AcademicSpaceDbContext db,
    IReservationRepository reservations,
    ISpaceRepository spaces)
{
    public async Task<ReservationDto> CreateAsync(CreateReservationRequest request, Guid solicitanteId)
    {
        ValidateCreate(request);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var space = await spaces.GetByIdForUpdateAsync(request.EspacioId)
                    ?? throw ApiException.NotFound("El espacio no fue encontrado.");
        if (!space.Activo)
        {
            throw ApiException.Conflict("El espacio no está disponible para reservas.");
        }
        if (await reservations.ExistsOverlapAsync(
                request.EspacioId, request.Fecha, request.HoraInicio, request.HoraFin))
        {
            throw ApiException.Conflict("El espacio ya está reservado en esa franja horaria.");
        }

        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            EspacioId = request.EspacioId,
            SolicitanteId = solicitanteId,
            Fecha = request.Fecha,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            Motivo = request.Motivo.Trim(),
            Estado = EstadoReserva.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            Version = 1
        };

        await reservations.AddAsync(reserva);
        await reservations.SaveChangesAsync();
        await transaction.CommitAsync();

        var created = await reservations.GetByIdAsync(reserva.Id, tracking: false)
                      ?? throw ApiException.NotFound("La solicitud no fue encontrada.");
        return ToDto(created);
    }

    public async Task<List<ReservationDto>> ListAsync(string role, Guid userId)
    {
        Guid? filter = role == SystemRoles.Solicitante ? userId : null;
        var list = await reservations.ListAsync(filter);
        return list.Select(ToDto).ToList();
    }

    public async Task<List<ReservationDto>> HistoryAsync(string role, Guid userId)
    {
        Guid? filter = role == SystemRoles.Solicitante ? userId : null;
        var list = await reservations.ListAsync(filter);
        return list.Select(ToDto).ToList();
    }

    public async Task<List<ReservationDto>> ListPendingAsync()
    {
        var list = await reservations.ListPendingAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ReservationDto> GetByIdAsync(Guid id, string role, Guid userId)
    {
        var reserva = await reservations.GetByIdAsync(id, tracking: false)
                      ?? throw ApiException.NotFound("La solicitud no fue encontrada.");

        if (role == SystemRoles.Solicitante && reserva.SolicitanteId != userId)
        {
            throw ApiException.Forbidden("No puede consultar solicitudes de otros usuarios.");
        }

        return ToDto(reserva);
    }

    public async Task<ReservationDto> ApproveAsync(Guid id, Guid aprobadorId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var reserva = await reservations.GetByIdAsync(id)
                      ?? throw ApiException.NotFound("La solicitud no fue encontrada.");

        if (!ReservationRules.CanTransition(reserva.Estado, EstadoReserva.Aprobada))
        {
            throw ApiException.Conflict("La solicitud ya no está pendiente.");
        }
        if (ReservationRules.ExceedsMaxDuration(reserva.HoraInicio, reserva.HoraFin))
        {
            throw ApiException.Conflict("La reserva no puede superar las cuatro horas consecutivas.");
        }

        await spaces.GetByIdForUpdateAsync(reserva.EspacioId);

        if (await reservations.ExistsOverlapAsync(
                reserva.EspacioId, reserva.Fecha, reserva.HoraInicio, reserva.HoraFin, reserva.Id))
        {
            throw ApiException.Conflict("La solicitud se superpone con otra reserva del mismo espacio.");
        }

        reserva.Estado = EstadoReserva.Aprobada;
        reserva.AprobadorId = aprobadorId;
        reserva.FechaDecision = DateTime.UtcNow;
        reserva.Version++;

        try
        {
            await reservations.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ApiException.Conflict("La solicitud fue modificada por otra operación. Intente nuevamente.");
        }

        var updated = await reservations.GetByIdAsync(id, tracking: false)
                      ?? throw ApiException.NotFound("La solicitud no fue encontrada.");
        return ToDto(updated);
    }

    public async Task<ReservationDto> RejectAsync(Guid id, Guid aprobadorId, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw ApiException.BadRequest("El motivo del rechazo es obligatorio.");
        }

        var reserva = await reservations.GetByIdAsync(id)
                      ?? throw ApiException.NotFound("La solicitud no fue encontrada.");

        if (!ReservationRules.CanTransition(reserva.Estado, EstadoReserva.Rechazada))
        {
            throw ApiException.Conflict("La solicitud ya no está pendiente.");
        }

        reserva.Estado = EstadoReserva.Rechazada;
        reserva.AprobadorId = aprobadorId;
        reserva.FechaDecision = DateTime.UtcNow;
        reserva.MotivoRechazo = motivo.Trim();
        reserva.Version++;

        try
        {
            await reservations.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ApiException.Conflict("La solicitud fue modificada por otra operación. Intente nuevamente.");
        }

        var updated = await reservations.GetByIdAsync(id, tracking: false)
                      ?? throw ApiException.NotFound("La solicitud no fue encontrada.");
        return ToDto(updated);
    }

    public async Task<ReservationDto> CancelAsync(Guid id, Guid solicitanteId)
    {
        var reserva = await reservations.GetByIdAsync(id)
                      ?? throw ApiException.NotFound("La solicitud no fue encontrada.");

        if (reserva.SolicitanteId != solicitanteId)
        {
            throw ApiException.Forbidden("No puede cancelar solicitudes de otros usuarios.");
        }
        if (!ReservationRules.CanTransition(reserva.Estado, EstadoReserva.Cancelada))
        {
            throw ApiException.Conflict("El estado actual no permite la cancelación.");
        }

        reserva.Estado = EstadoReserva.Cancelada;
        reserva.FechaCancelacion = DateTime.UtcNow;
        reserva.Version++;

        try
        {
            await reservations.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ApiException.Conflict("La solicitud fue modificada por otra operación. Intente nuevamente.");
        }

        var updated = await reservations.GetByIdAsync(id, tracking: false)
                      ?? throw ApiException.NotFound("La solicitud no fue encontrada.");
        return ToDto(updated);
    }

    private static void ValidateCreate(CreateReservationRequest request)
    {
        if (request.EspacioId == Guid.Empty)
        {
            throw ApiException.BadRequest("El espacio es obligatorio.");
        }
        if (request.Fecha == default)
        {
            throw ApiException.BadRequest("La fecha es obligatoria.");
        }
        if (request.HoraFin <= request.HoraInicio)
        {
            throw ApiException.BadRequest("La hora de fin debe ser posterior a la hora de inicio.");
        }
        if (ReservationRules.ExceedsMaxDuration(request.HoraInicio, request.HoraFin))
        {
            throw ApiException.BadRequest("La reserva no puede superar las cuatro horas consecutivas.");
        }
        if (ReservationRules.IsPast(request.Fecha, request.HoraInicio))
        {
            throw ApiException.BadRequest("La reserva debe ser para una fecha y hora futuras.");
        }
        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw ApiException.BadRequest("El motivo es obligatorio.");
        }
    }

    private static ReservationDto ToDto(Reserva reserva) => new(
        reserva.Id,
        reserva.EspacioId,
        reserva.Espacio.Nombre,
        reserva.Espacio.Tipo.ToString(),
        reserva.SolicitanteId,
        reserva.Solicitante.Nombre,
        reserva.Fecha,
        reserva.HoraInicio,
        reserva.HoraFin,
        reserva.Motivo,
        reserva.Estado.ToString(),
        reserva.FechaSolicitud,
        reserva.AprobadorId,
        reserva.Aprobador?.Nombre,
        reserva.FechaDecision,
        reserva.MotivoRechazo,
        reserva.FechaCancelacion);
}
