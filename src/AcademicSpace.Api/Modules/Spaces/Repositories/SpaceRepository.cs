using AcademicSpace.Api.Modules.Spaces.Entities;
using AcademicSpace.Api.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSpace.Api.Modules.Spaces.Repositories;

public class SpaceRepository(AcademicSpaceDbContext db) : ISpaceRepository
{
    public async Task<Espacio?> GetByIdAsync(Guid id, bool tracking = true)
    {
        var query = db.Espacios.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }
        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Espacio?> GetByIdForUpdateAsync(Guid id)
    {
        var spaces = await db.Espacios
            .FromSqlRaw("SELECT * FROM Espacio WHERE Id = {0} FOR UPDATE", id)
            .ToListAsync();
        return spaces.FirstOrDefault();
    }

    public async Task<List<Espacio>> ListAsync(bool includeInactive, TipoEspacio? tipo = null)
    {
        var query = db.Espacios.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(e => e.Activo);
        }
        if (tipo is not null)
        {
            query = query.Where(e => e.Tipo == tipo);
        }
        return await query.OrderBy(e => e.Nombre).ToListAsync();
    }

    public Task<bool> NameExistsAsync(string nombre, Guid? excludeId = null) =>
        db.Espacios.AnyAsync(e => e.Nombre == nombre && (excludeId == null || e.Id != excludeId));

    public async Task AddAsync(Espacio espacio) => await db.Espacios.AddAsync(espacio);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
