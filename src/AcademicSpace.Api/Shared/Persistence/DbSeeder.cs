using System.Security.Cryptography;
using AcademicSpace.Api.Modules.Users.Entities;
using AcademicSpace.Api.Shared.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademicSpace.Api.Shared.Persistence;

public static class DbSeeder
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademicSpaceDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DbSeeder));

        await db.Database.MigrateAsync();

        foreach (var roleName in SystemRoles.All)
        {
            var roleId = SystemRoles.GetId(roleName)!.Value;
            if (!await db.Roles.AnyAsync(r => r.Id == roleId))
            {
                db.Roles.Add(new Rol { Id = roleId, Nombre = roleName });
            }
        }
        await db.SaveChangesAsync();

        var adminEmail = (config["Seed:AdminEmail"] ?? "admin@academicspace.edu").Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Correo == adminEmail))
        {
            return;
        }

        var adminPassword = config["Seed:AdminPassword"];
        var generated = false;
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            adminPassword = GeneratePassword();
            generated = true;
        }

        var admin = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = config["Seed:AdminName"] ?? "Administrador",
            Correo = adminEmail,
            RolId = SystemRoles.GetId(SystemRoles.Administrador)!.Value,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow
        };
        admin.HashContrasena = new PasswordHasher<Usuario>().HashPassword(admin, adminPassword);

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        if (generated)
        {
            logger.LogWarning("Administrador inicial creado: {Correo}. Contraseña generada: {Password}. " +
                              "Configure Seed__AdminPassword para evitar este mensaje.", adminEmail, adminPassword);
        }
    }

    private static string GeneratePassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(16);
        return new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
    }
}
