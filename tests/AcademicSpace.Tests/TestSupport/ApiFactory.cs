using AcademicSpace.Api.Modules.Users.Entities;
using AcademicSpace.Api.Shared.Auth;
using AcademicSpace.Api.Shared.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicSpace.Tests.TestSupport;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string SolicitanteEmail = "solicitante@test.local";
    public const string AprobadorEmail = "aprobador@test.local";
    public const string Password = "Password123!";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private bool _initialized;

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ACADEMICSPACE_TEST_DB")
        ?? "Server=127.0.0.1;Port=3310;Database=academicspace_test;User=root;Password=academicspace;AllowUserVariables=true";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                ["Jwt:Issuer"] = "AcademicSpace.Tests",
                ["Jwt:Audience"] = "AcademicSpace.Tests",
                ["Jwt:Key"] = "clave-de-pruebas-academicspace-con-longitud-suficiente",
                ["Jwt:ExpirationHours"] = "8",
                ["Seed:AdminName"] = "Administrador de Pruebas",
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = Password,
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
            });
        });
    }

    public async Task ResetAsync()
    {
        await Gate.WaitAsync();
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademicSpaceDbContext>();

            if (!_initialized)
            {
                await db.Database.MigrateAsync();
                _initialized = true;
            }

            await db.Database.ExecuteSqlRawAsync("DELETE FROM `Reserva`");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM `Sesion`");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM `Usuario`");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM `Espacio`");

            foreach (var role in SystemRoles.All)
            {
                var id = SystemRoles.GetId(role)!.Value;
                if (!await db.Roles.AnyAsync(r => r.Id == id))
                {
                    db.Roles.Add(new Rol { Id = id, Nombre = role });
                }
            }
            await db.SaveChangesAsync();

            AddUser(db, AdminEmail, "Administrador de Pruebas", SystemRoles.Administrador);
            AddUser(db, SolicitanteEmail, "Solicitante de Pruebas", SystemRoles.Solicitante);
            AddUser(db, AprobadorEmail, "Aprobador de Pruebas", SystemRoles.Aprobador);
            await db.SaveChangesAsync();
        }
        finally
        {
            Gate.Release();
        }
    }

    private static void AddUser(AcademicSpaceDbContext db, string email, string name, string role)
    {
        var user = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = name,
            Correo = email,
            RolId = SystemRoles.GetId(role)!.Value,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow
        };
        user.HashContrasena = new PasswordHasher<Usuario>().HashPassword(user, Password);
        db.Usuarios.Add(user);
    }
}
