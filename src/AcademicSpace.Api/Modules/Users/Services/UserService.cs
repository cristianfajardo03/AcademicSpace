using AcademicSpace.Api.Modules.Users.Contracts;
using AcademicSpace.Api.Modules.Users.Entities;
using AcademicSpace.Api.Modules.Users.Repositories;
using AcademicSpace.Api.Shared.Auth;
using AcademicSpace.Api.Shared.Errors;
using Microsoft.AspNetCore.Identity;

namespace AcademicSpace.Api.Modules.Users.Services;

public class UserService(IUserRepository users)
{
    private static readonly PasswordHasher<Usuario> PasswordHasher = new();

    public async Task<List<UserDto>> GetAllAsync()
    {
        var list = await users.ListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await users.GetByIdAsync(id, tracking: false)
                   ?? throw ApiException.NotFound("El usuario no fue encontrado.");
        return ToDto(user);
    }

    public async Task<UserDto> RegisterAsync(CreateUserRequest request)
    {
        var nombre = request.Nombre.Trim();
        var correo = NormalizeEmail(request.Correo);

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw ApiException.BadRequest("El nombre es obligatorio.");
        }
        if (request.Contrasena.Length < 8)
        {
            throw ApiException.BadRequest("La contraseña debe tener al menos ocho caracteres.");
        }
        var rolId = SystemRoles.GetId(request.Rol)
                    ?? throw ApiException.BadRequest("El rol indicado no es válido.");
        if (await users.EmailExistsAsync(correo))
        {
            throw ApiException.Conflict("El correo ya está en uso.");
        }

        var user = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Correo = correo,
            RolId = rolId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow
        };
        user.HashContrasena = PasswordHasher.HashPassword(user, request.Contrasena);

        await users.AddAsync(user);
        await users.SaveChangesAsync();

        var created = await users.GetByIdAsync(user.Id, tracking: false)
                      ?? throw ApiException.NotFound("El usuario no fue encontrado.");
        return ToDto(created);
    }

    public async Task<UserDto> UpdateByAdminAsync(Guid id, UpdateUserRequest request)
    {
        var user = await users.GetByIdAsync(id)
                   ?? throw ApiException.NotFound("El usuario no fue encontrado.");

        var nombre = request.Nombre.Trim();
        var correo = NormalizeEmail(request.Correo);
        var rolId = SystemRoles.GetId(request.Rol)
                    ?? throw ApiException.BadRequest("El rol indicado no es válido.");

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw ApiException.BadRequest("El nombre es obligatorio.");
        }
        if (await users.EmailExistsAsync(correo, id))
        {
            throw ApiException.Conflict("El correo ya está en uso.");
        }

        var wasActive = user.Activo;
        user.Nombre = nombre;
        user.Correo = correo;
        user.RolId = rolId;
        user.Activo = request.Activo;
        user.FechaActualizacion = DateTime.UtcNow;

        await users.SaveChangesAsync();

        if (wasActive && !user.Activo)
        {
            await users.RevokeSessionsAsync(user.Id);
        }

        var updated = await users.GetByIdAsync(user.Id, tracking: false)
                      ?? throw ApiException.NotFound("El usuario no fue encontrado.");
        return ToDto(updated);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid id, UpdateProfileRequest request)
    {
        if (request.Rol is not null || request.Activo is not null)
        {
            throw ApiException.Forbidden("No puede modificar su propio rol ni su estado.");
        }

        var user = await users.GetByIdAsync(id)
                   ?? throw ApiException.NotFound("El usuario no fue encontrado.");

        var nombre = request.Nombre.Trim();
        var correo = NormalizeEmail(request.Correo);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw ApiException.BadRequest("El nombre es obligatorio.");
        }
        if (await users.EmailExistsAsync(correo, id))
        {
            throw ApiException.Conflict("El correo ya está en uso.");
        }

        user.Nombre = nombre;
        user.Correo = correo;
        user.FechaActualizacion = DateTime.UtcNow;
        await users.SaveChangesAsync();

        var updated = await users.GetByIdAsync(user.Id, tracking: false)
                      ?? throw ApiException.NotFound("El usuario no fue encontrado.");
        return ToDto(updated);
    }

    private static string NormalizeEmail(string correo) => correo.Trim().ToLowerInvariant();

    private static UserDto ToDto(Usuario user) => new(
        user.Id,
        user.Nombre,
        user.Correo,
        user.Rol.Nombre,
        user.Activo,
        user.FechaCreacion);
}
