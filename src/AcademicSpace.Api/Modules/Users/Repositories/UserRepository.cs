using AcademicSpace.Api.Modules.Users.Entities;
using AcademicSpace.Api.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSpace.Api.Modules.Users.Repositories;

public class UserRepository(AcademicSpaceDbContext db) : IUserRepository
{
    public async Task<Usuario?> GetByIdAsync(Guid id, bool tracking = true)
    {
        var query = db.Usuarios.Include(u => u.Rol).AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }
        return await query.FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<Usuario?> GetByEmailAsync(string correo) =>
        db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Correo == correo);

    public async Task<List<Usuario>> ListAsync() =>
        await db.Usuarios.Include(u => u.Rol).AsNoTracking()
            .OrderBy(u => u.Nombre).ThenBy(u => u.Correo).ToListAsync();

    public Task<bool> EmailExistsAsync(string correo, Guid? excludeId = null) =>
        db.Usuarios.AnyAsync(u => u.Correo == correo && (excludeId == null || u.Id != excludeId));

    public async Task AddAsync(Usuario usuario) => await db.Usuarios.AddAsync(usuario);

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public Task RevokeSessionsAsync(Guid usuarioId) =>
        db.Sesiones.Where(s => s.UsuarioId == usuarioId && !s.Revocada)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Revocada, true));

    public async Task AddSessionAsync(Sesion sesion) => await db.Sesiones.AddAsync(sesion);

    public async Task RevokeSessionAsync(string sessionId)
    {
        var session = await db.Sesiones.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is not null)
        {
            session.Revocada = true;
        }
    }
}
