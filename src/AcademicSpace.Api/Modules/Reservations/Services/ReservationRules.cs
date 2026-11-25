using AcademicSpace.Api.Modules.Reservations.Entities;

namespace AcademicSpace.Api.Modules.Reservations.Services;

public static class ReservationRules
{
    public static readonly TimeSpan MaxDuracion = TimeSpan.FromHours(4);

    public static bool ExceedsMaxDuration(TimeOnly inicio, TimeOnly fin) =>
        fin - inicio > MaxDuracion;

    public static bool Overlaps(
        DateOnly fecha, TimeOnly inicio, TimeOnly fin,
        DateOnly otraFecha, TimeOnly otroInicio, TimeOnly otroFin) =>
        fecha == otraFecha && inicio < otroFin && fin > otroInicio;

    public static bool CanTransition(EstadoReserva actual, EstadoReserva destino) =>
        (actual, destino) switch
        {
            (EstadoReserva.Pendiente, EstadoReserva.Aprobada) => true,
            (EstadoReserva.Pendiente, EstadoReserva.Rechazada) => true,
            (EstadoReserva.Pendiente, EstadoReserva.Cancelada) => true,
            (EstadoReserva.Aprobada, EstadoReserva.Cancelada) => true,
            _ => false
        };

    public static bool IsPast(DateOnly fecha, TimeOnly inicio)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        return fecha < today || (fecha == today && inicio <= TimeOnly.FromDateTime(now));
    }
}
