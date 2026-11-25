using AcademicSpace.Api.Modules.Reservations.Contracts;
using AcademicSpace.Api.Modules.Reservations.Services;
using AcademicSpace.Api.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSpace.Api.Modules.Reservations.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController(ReservationService reservations) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = SystemRoles.Solicitante)]
    public async Task<ActionResult<ReservationDto>> Create(CreateReservationRequest request)
    {
        var created = await reservations.CreateAsync(request, User.GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<List<ReservationDto>>> GetAll() =>
        Ok(await reservations.ListAsync(User.GetRole(), User.GetUserId()));

    [HttpGet("pending")]
    [Authorize(Roles = SystemRoles.Aprobador)]
    public async Task<ActionResult<List<ReservationDto>>> GetPending() =>
        Ok(await reservations.ListPendingAsync());

    [HttpGet("history")]
    public async Task<ActionResult<List<ReservationDto>>> GetHistory() =>
        Ok(await reservations.HistoryAsync(User.GetRole(), User.GetUserId()));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> GetById(Guid id) =>
        Ok(await reservations.GetByIdAsync(id, User.GetRole(), User.GetUserId()));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = SystemRoles.Aprobador)]
    public async Task<ActionResult<ReservationDto>> Approve(Guid id) =>
        Ok(await reservations.ApproveAsync(id, User.GetUserId()));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = SystemRoles.Aprobador)]
    public async Task<ActionResult<ReservationDto>> Reject(Guid id, RejectReservationRequest request) =>
        Ok(await reservations.RejectAsync(id, User.GetUserId(), request.Motivo));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = SystemRoles.Solicitante)]
    public async Task<ActionResult<ReservationDto>> Cancel(Guid id) =>
        Ok(await reservations.CancelAsync(id, User.GetUserId()));
}
