namespace AcademicSpace.Api.Modules.Reservations.Entities;

using AcademicSpace.Api.Modules.Spaces.Entities;
using AcademicSpace.Api.Modules.Users.Entities;

public class Reserva
{
    public Guid Id { get; set; }
    public Guid EspacioId { get; set; }
    public Espacio Espacio { get; set; } = null!;
    public Guid SolicitanteId { get; set; }
    public Usuario Solicitante { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
    public DateTime FechaSolicitud { get; set; }
    public Guid? AprobadorId { get; set; }
    public Usuario? Aprobador { get; set; }
    public DateTime? FechaDecision { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public int Version { get; set; } = 1;
}
