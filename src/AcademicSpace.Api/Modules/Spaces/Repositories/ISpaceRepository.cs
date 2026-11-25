using AcademicSpace.Api.Modules.Spaces.Entities;

namespace AcademicSpace.Api.Modules.Spaces.Repositories;

public interface ISpaceRepository
{
    Task<Espacio?> GetByIdAsync(Guid id, bool tracking = true);
    Task<Espacio?> GetByIdForUpdateAsync(Guid id);
    Task<List<Espacio>> ListAsync(bool includeInactive, TipoEspacio? tipo = null);
    Task<bool> NameExistsAsync(string nombre, Guid? excludeId = null);
    Task AddAsync(Espacio espacio);
    Task SaveChangesAsync();
}
