namespace AcademicSpace.Api.Modules.Users.Entities;

public class Sesion
{
    public string Id { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public bool Revocada { get; set; }
}
