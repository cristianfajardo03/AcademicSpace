using AcademicSpace.Api.Modules.Reservations.Entities;
using AcademicSpace.Api.Modules.Spaces.Entities;
using AcademicSpace.Api.Modules.Users.Entities;
using AcademicSpace.Api.Shared.Auth;
using Microsoft.EntityFrameworkCore;

namespace AcademicSpace.Api.Shared.Persistence;

public class AcademicSpaceDbContext(DbContextOptions<AcademicSpaceDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Espacio> Espacios => Set<Espacio>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Rol");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Nombre).HasMaxLength(40).IsRequired();
            entity.HasIndex(r => r.Nombre).IsUnique();
            entity.HasData(
                new Rol { Id = 1, Nombre = SystemRoles.Administrador },
                new Rol { Id = 2, Nombre = SystemRoles.Solicitante },
                new Rol { Id = 3, Nombre = SystemRoles.Aprobador });
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Nombre).HasMaxLength(120).IsRequired();
            entity.Property(u => u.Correo).HasMaxLength(200).IsRequired();
            entity.Property(u => u.HashContrasena).HasMaxLength(500).IsRequired();
            entity.HasIndex(u => u.Correo).IsUnique();
            entity.HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Espacio>(entity =>
        {
            entity.ToTable("Espacio");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.Ubicacion).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("Reserva");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(r => r.Motivo).HasMaxLength(500).IsRequired();
            entity.Property(r => r.MotivoRechazo).HasMaxLength(500);
            entity.Property(r => r.Version).IsConcurrencyToken();
            entity.HasIndex(r => new { r.EspacioId, r.Fecha, r.Estado });
            entity.HasIndex(r => new { r.SolicitanteId, r.FechaSolicitud });
            entity.HasOne(r => r.Espacio)
                .WithMany(e => e.Reservas)
                .HasForeignKey(r => r.EspacioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Solicitante)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.SolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Aprobador)
                .WithMany()
                .HasForeignKey(r => r.AprobadorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sesion>(entity =>
        {
            entity.ToTable("Sesion");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).HasMaxLength(64);
            entity.HasIndex(s => s.UsuarioId);
            entity.HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
