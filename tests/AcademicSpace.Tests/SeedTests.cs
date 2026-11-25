using AcademicSpace.Api.Shared.Persistence;
using AcademicSpace.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class SeedTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Siembra_ContieneLosTresRolesUnicosYElAdministradorConfigurado()
    {
        await Factory.Services.InitializeDatabaseAsync();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademicSpaceDbContext>();

        var roles = await db.Roles.AsNoTracking().Select(r => r.Nombre).ToListAsync();
        Assert.Equal(3, roles.Count);
        Assert.Equal(roles.Count, roles.Distinct().Count());
        Assert.Contains("Administrador", roles);
        Assert.Contains("Solicitante", roles);
        Assert.Contains("Aprobador", roles);

        var admin = await db.Usuarios.AsNoTracking()
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Correo == ApiFactory.AdminEmail);
        Assert.NotNull(admin);
        Assert.Equal("Administrador", admin!.Rol.Nombre);
        Assert.True(admin.Activo);
    }

    [Fact]
    public async Task Siembra_EsIdempotente()
    {
        await Factory.Services.InitializeDatabaseAsync();
        await Factory.Services.InitializeDatabaseAsync();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademicSpaceDbContext>();

        Assert.Equal(3, await db.Roles.CountAsync());
        Assert.Equal(1, await db.Usuarios.CountAsync(u => u.Correo == ApiFactory.AdminEmail));
    }
}
