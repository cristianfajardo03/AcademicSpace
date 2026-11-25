using System.Security.Claims;

namespace AcademicSpace.Api.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("La sesión no contiene un usuario válido.");
    }

    public static string GetRole(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public static bool IsAdministrador(this ClaimsPrincipal principal) =>
        principal.IsInRole(SystemRoles.Administrador);

    public static bool IsSolicitante(this ClaimsPrincipal principal) =>
        principal.IsInRole(SystemRoles.Solicitante);

    public static bool IsAprobador(this ClaimsPrincipal principal) =>
        principal.IsInRole(SystemRoles.Aprobador);
}
