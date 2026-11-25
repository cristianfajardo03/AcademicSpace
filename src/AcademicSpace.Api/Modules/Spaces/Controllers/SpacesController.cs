using AcademicSpace.Api.Modules.Spaces.Contracts;
using AcademicSpace.Api.Modules.Spaces.Entities;
using AcademicSpace.Api.Modules.Spaces.Services;
using AcademicSpace.Api.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSpace.Api.Modules.Spaces.Controllers;

[ApiController]
[Route("api/spaces")]
[Authorize]
public class SpacesController(SpaceService spaces) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SpaceDto>>> GetAll([FromQuery] TipoEspacio? tipo) =>
        Ok(await spaces.GetAllAsync(User.IsAdministrador(), tipo));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SpaceDto>> GetById(Guid id) =>
        Ok(await spaces.GetByIdAsync(id, User.IsAdministrador()));

    [HttpPost]
    [Authorize(Roles = SystemRoles.Administrador)]
    public async Task<ActionResult<SpaceDto>> Create(CreateSpaceRequest request)
    {
        var created = await spaces.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = SystemRoles.Administrador)]
    public async Task<ActionResult<SpaceDto>> Update(Guid id, UpdateSpaceRequest request) =>
        Ok(await spaces.UpdateAsync(id, request));
}
