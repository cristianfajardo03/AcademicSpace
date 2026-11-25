namespace AcademicSpace.Api.Modules.Spaces.Entities;

using AcademicSpace.Api.Modules.Reservations.Entities;

public class Espacio
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoEspacio Tipo { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
