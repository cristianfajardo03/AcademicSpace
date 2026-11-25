using AcademicSpace.Api.Modules.Availability.Contracts;
using AcademicSpace.Api.Modules.Availability.Services;
using AcademicSpace.Api.Modules.Spaces.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSpace.Api.Modules.Availability.Controllers;

[ApiController]
[Route("api/availability")]
[Authorize]
public class AvailabilityController(AvailabilityService availability) : ControllerBase
{
    [HttpGet("spaces")]
    public async Task<ActionResult<AvailableSpacesResponse>> GetAvailableSpaces(
        [FromQuery] DateOnly fecha,
        [FromQuery] TimeOnly horaInicio,
        [FromQuery] TimeOnly horaFin,
        [FromQuery] TipoEspacio? tipo) =>
        Ok(await availability.GetAvailableSpacesAsync(fecha, horaInicio, horaFin, tipo));

    [HttpGet("spaces/{id:guid}")]
    public async Task<ActionResult<SpaceAvailabilityDto>> GetSpaceAvailability(
        Guid id,
        [FromQuery] DateOnly fecha,
        [FromQuery] TimeOnly horaInicio,
        [FromQuery] TimeOnly horaFin) =>
        Ok(await availability.GetSpaceAvailabilityAsync(id, fecha, horaInicio, horaFin));
}
