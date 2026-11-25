using AcademicSpace.Api.Modules.Users.Entities;

namespace AcademicSpace.Api.Modules.Users.Repositories;

public interface IUserRepository
{
    Task<Usuario?> GetByIdAsync(Guid id, bool tracking = true);
    Task<Usuario?> GetByEmailAsync(string correo);
    Task<List<Usuario>> ListAsync();
    Task<bool> EmailExistsAsync(string correo, Guid? excludeId = null);
    Task AddAsync(Usuario usuario);
    Task SaveChangesAsync();
    Task RevokeSessionsAsync(Guid usuarioId);
    Task AddSessionAsync(Sesion sesion);
    Task RevokeSessionAsync(string sessionId);
}
