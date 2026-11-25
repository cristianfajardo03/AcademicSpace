using AcademicSpace.Api.Modules.Availability.Contracts;
using AcademicSpace.Api.Modules.Reservations.Repositories;
using AcademicSpace.Api.Modules.Reservations.Services;
using AcademicSpace.Api.Modules.Spaces.Contracts;
using AcademicSpace.Api.Modules.Spaces.Entities;
using AcademicSpace.Api.Modules.Spaces.Repositories;
using AcademicSpace.Api.Shared.Errors;

namespace AcademicSpace.Api.Modules.Availability.Services;

public class AvailabilityService(
    ISpaceRepository spaces,
    IReservationRepository reservations)
{
    public async Task<AvailableSpacesResponse> GetAvailableSpacesAsync(
        DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin, TipoEspacio? tipo)
    {
        Validate(fecha, horaInicio, horaFin, tipo);

        var all = await spaces.ListAsync(includeInactive: false, tipo);
        var occupied = await reservations.GetOccupiedSpaceIdsAsync(fecha, horaInicio, horaFin);
        var available = all.Where(s => !occupied.Contains(s.Id)).ToList();

        var dtos = available.Select(s => new SpaceDto(
            s.Id, s.Nombre, s.Tipo.ToString(), s.Ubicacion, s.Capacidad, s.Activo)).ToList();

        return new AvailableSpacesResponse(
            dtos,
            dtos.Count == 0 ? "No hay espacios disponibles en la franja consultada." : "Espacios disponibles.");
    }

    public async Task<SpaceAvailabilityDto> GetSpaceAvailabilityAsync(
        Guid espacioId, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
    {
        Validate(fecha, horaInicio, horaFin, tipo: null);

        var space = await spaces.GetByIdAsync(espacioId, tracking: false)
                    ?? throw ApiException.NotFound("El espacio no fue encontrado.");

        if (!space.Activo)
        {
            return new SpaceAvailabilityDto(space.Id, space.Nombre, false, []);
        }

        var overlaps = await reservations.GetOverlapsAsync(espacioId, fecha, horaInicio, horaFin);
        var intervals = overlaps
            .Select(r => new OccupiedIntervalDto(r.HoraInicio, r.HoraFin))
            .ToList();

        return new SpaceAvailabilityDto(space.Id, space.Nombre, intervals.Count == 0, intervals);
    }

    private static void Validate(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin, TipoEspacio? tipo)
    {
        if (tipo is not null && !Enum.IsDefined(tipo.Value))
        {
            throw ApiException.BadRequest("El tipo de espacio no es válido.");
        }
        if (fecha == default)
        {
            throw ApiException.BadRequest("La fecha es obligatoria.");
        }
        if (horaFin <= horaInicio)
        {
            throw ApiException.BadRequest("La hora de fin debe ser posterior a la hora de inicio.");
        }
        if (ReservationRules.ExceedsMaxDuration(horaInicio, horaFin))
        {
            throw ApiException.BadRequest("La franja consultada no puede superar las cuatro horas consecutivas.");
        }
        if (ReservationRules.IsPast(fecha, horaInicio))
        {
            throw ApiException.BadRequest("La fecha y hora deben ser futuras.");
        }
    }
}
