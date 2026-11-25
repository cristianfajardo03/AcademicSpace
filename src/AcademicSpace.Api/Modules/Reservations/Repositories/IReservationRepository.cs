using AcademicSpace.Api.Modules.Reservations.Entities;

namespace AcademicSpace.Api.Modules.Reservations.Repositories;

public interface IReservationRepository
{
    Task<Reserva?> GetByIdAsync(Guid id, bool tracking = true);
    Task<List<Reserva>> ListAsync(Guid? solicitanteId = null, bool pendingOnly = false);
    Task<List<Reserva>> ListPendingAsync();
    Task<bool> ExistsOverlapAsync(Guid espacioId, DateOnly fecha, TimeOnly inicio, TimeOnly fin,
        Guid? excludeId = null);
    Task<List<Reserva>> GetOverlapsAsync(Guid espacioId, DateOnly fecha, TimeOnly inicio, TimeOnly fin);
    Task<HashSet<Guid>> GetOccupiedSpaceIdsAsync(DateOnly fecha, TimeOnly inicio, TimeOnly fin);
    Task AddAsync(Reserva reserva);
    Task SaveChangesAsync();
}
