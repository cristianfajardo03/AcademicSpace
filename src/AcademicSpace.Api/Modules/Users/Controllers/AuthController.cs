using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AcademicSpace.Api.Modules.Users.Contracts;
using AcademicSpace.Api.Modules.Users.Services;
using AcademicSpace.Api.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSpace.Api.Modules.Users.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request) =>
        Ok(await auth.LoginAsync(request));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var sessionId = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw ApiException.Unauthorized("La sesión no es válida.");
        }
        await auth.LogoutAsync(sessionId);
        return NoContent();
    }
}
