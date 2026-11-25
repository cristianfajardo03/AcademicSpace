using System.ComponentModel.DataAnnotations;

namespace AcademicSpace.Api.Modules.Users.Contracts;

public record LoginRequest(
    [Required, EmailAddress] string Correo,
    [Required] string Contrasena);

public record LoginResponse(string Token, DateTime Expiracion, UserDto Usuario);

public record UserDto(
    Guid Id,
    string Nombre,
    string Correo,
    string Rol,
    bool Activo,
    DateTime FechaCreacion);

public record CreateUserRequest(
    [Required, StringLength(120)] string Nombre,
    [Required, EmailAddress, StringLength(200)] string Correo,
    [Required, MinLength(8), StringLength(100)] string Contrasena,
    [Required] string Rol);

public record UpdateUserRequest(
    [Required, StringLength(120)] string Nombre,
    [Required, EmailAddress, StringLength(200)] string Correo,
    [Required] string Rol,
    bool Activo);

public record UpdateProfileRequest(
    [Required, StringLength(120)] string Nombre,
    [Required, EmailAddress, StringLength(200)] string Correo,
    string? Rol = null,
    bool? Activo = null);
