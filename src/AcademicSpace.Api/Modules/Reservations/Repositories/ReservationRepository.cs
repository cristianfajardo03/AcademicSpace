using AcademicSpace.Api.Modules.Reservations.Entities;
using AcademicSpace.Api.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSpace.Api.Modules.Reservations.Repositories;

public class ReservationRepository(AcademicSpaceDbContext db) : IReservationRepository
{
    private static readonly EstadoReserva[] BlockingStates = [EstadoReserva.Pendiente, EstadoReserva.Aprobada];

    public async Task<Reserva?> GetByIdAsync(Guid id, bool tracking = true)
    {
        var query = db.Reservas
            .Include(r => r.Espacio)
            .Include(r => r.Solicitante)
            .Include(r => r.Aprobador)
            .AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }
        return await query.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Reserva>> ListAsync(Guid? solicitanteId = null, bool pendingOnly = false)
    {
        var query = db.Reservas
            .Include(r => r.Espacio)
            .Include(r => r.Solicitante)
            .Include(r => r.Aprobador)
            .AsNoTracking()
            .AsQueryable();

        if (solicitanteId is not null)
        {
            query = query.Where(r => r.SolicitanteId == solicitanteId);
        }
        if (pendingOnly)
        {
            query = query.Where(r => r.Estado == EstadoReserva.Pendiente);
        }

        return await query.OrderByDescending(r => r.FechaSolicitud).ThenBy(r => r.Id).ToListAsync();
    }

    public async Task<List<Reserva>> ListPendingAsync() =>
        await db.Reservas
            .Include(r => r.Espacio)
            .Include(r => r.Solicitante)
            .AsNoTracking()
            .Where(r => r.Estado == EstadoReserva.Pendiente)
            .OrderBy(r => r.FechaSolicitud).ThenBy(r => r.Id)
            .ToListAsync();

    public Task<bool> ExistsOverlapAsync(Guid espacioId, DateOnly fecha, TimeOnly inicio, TimeOnly fin,
        Guid? excludeId = null) =>
        db.Reservas.AnyAsync(r =>
            r.EspacioId == espacioId &&
            r.Fecha == fecha &&
            BlockingStates.Contains(r.Estado) &&
            r.HoraInicio < fin &&
            r.HoraFin > inicio &&
            (excludeId == null || r.Id != excludeId));

    public async Task<List<Reserva>> GetOverlapsAsync(Guid espacioId, DateOnly fecha, TimeOnly inicio, TimeOnly fin) =>
        await db.Reservas.AsNoTracking()
            .Where(r =>
                r.EspacioId == espacioId &&
                r.Fecha == fecha &&
                BlockingStates.Contains(r.Estado) &&
                r.HoraInicio < fin &&
                r.HoraFin > inicio)
            .OrderBy(r => r.HoraInicio)
            .ToListAsync();

    public async Task<HashSet<Guid>> GetOccupiedSpaceIdsAsync(DateOnly fecha, TimeOnly inicio, TimeOnly fin)
    {
        var ids = await db.Reservas.AsNoTracking()
            .Where(r =>
                r.Fecha == fecha &&
                BlockingStates.Contains(r.Estado) &&
                r.HoraInicio < fin &&
                r.HoraFin > inicio)
            .Select(r => r.EspacioId)
            .Distinct()
            .ToListAsync();
        return ids.ToHashSet();
    }

    public async Task AddAsync(Reserva reserva) => await db.Reservas.AddAsync(reserva);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
