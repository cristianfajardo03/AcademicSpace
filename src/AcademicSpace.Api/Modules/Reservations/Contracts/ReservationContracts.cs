using System.ComponentModel.DataAnnotations;

namespace AcademicSpace.Api.Modules.Reservations.Contracts;

public record CreateReservationRequest(
    Guid EspacioId,
    DateOnly Fecha,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    [Required, StringLength(500)] string Motivo);

public record RejectReservationRequest(
    [Required, StringLength(500)] string Motivo);

public record ReservationDto(
    Guid Id,
    Guid EspacioId,
    string EspacioNombre,
    string EspacioTipo,
    Guid SolicitanteId,
    string SolicitanteNombre,
    DateOnly Fecha,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    string Motivo,
    string Estado,
    DateTime FechaSolicitud,
    Guid? AprobadorId,
    string? AprobadorNombre,
    DateTime? FechaDecision,
    string? MotivoRechazo,
    DateTime? FechaCancelacion);
