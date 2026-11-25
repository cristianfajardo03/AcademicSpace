using AcademicSpace.Api.Modules.Users.Contracts;
using AcademicSpace.Api.Modules.Users.Entities;
using AcademicSpace.Api.Modules.Users.Repositories;
using AcademicSpace.Api.Shared.Auth;
using AcademicSpace.Api.Shared.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AcademicSpace.Api.Modules.Users.Services;

public class AuthService(
    IUserRepository users,
    TokenService tokens,
    IOptions<JwtOptions> jwtOptions)
{
    private static readonly PasswordHasher<Usuario> PasswordHasher = new();
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(correo);
        if (user is null)
        {
            throw ApiException.Unauthorized("Credenciales inválidas.");
        }

        var result = PasswordHasher.VerifyHashedPassword(user, user.HashContrasena, request.Contrasena);
        if (result == PasswordVerificationResult.Failed)
        {
            throw ApiException.Unauthorized("Credenciales inválidas.");
        }

        if (!user.Activo)
        {
            throw ApiException.Unauthorized("La cuenta no está habilitada.");
        }

        var now = DateTime.UtcNow;
        var session = new Sesion
        {
            Id = Guid.NewGuid().ToString("N"),
            UsuarioId = user.Id,
            FechaInicio = now,
            FechaExpiracion = now.AddHours(_jwtOptions.ExpirationHours),
            Revocada = false
        };

        await users.AddSessionAsync(session);
        await users.SaveChangesAsync();

        var (token, expiration) = tokens.Create(user, session);
        return new LoginResponse(token, expiration, new UserDto(
            user.Id, user.Nombre, user.Correo, user.Rol.Nombre, user.Activo, user.FechaCreacion));
    }

    public async Task LogoutAsync(string sessionId)
    {
        await users.RevokeSessionAsync(sessionId);
        await users.SaveChangesAsync();
    }
}
