using AcademicSpace.Api.Modules.Users.Contracts;
using AcademicSpace.Api.Modules.Users.Services;
using AcademicSpace.Api.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSpace.Api.Modules.Users.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(UserService users) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = SystemRoles.Administrador)]
    public async Task<ActionResult<List<UserDto>>> GetAll() =>
        Ok(await users.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = SystemRoles.Administrador)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var created = await users.RegisterAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe() =>
        Ok(await users.GetByIdAsync(User.GetUserId()));

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest request) =>
        Ok(await users.UpdateProfileAsync(User.GetUserId(), request));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = SystemRoles.Administrador)]
    public async Task<ActionResult<UserDto>> GetById(Guid id) =>
        Ok(await users.GetByIdAsync(id));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = SystemRoles.Administrador)]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest request) =>
        Ok(await users.UpdateByAdminAsync(id, request));
}
