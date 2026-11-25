using AcademicSpace.Api.Modules.Spaces.Contracts;
using AcademicSpace.Api.Modules.Spaces.Entities;
using AcademicSpace.Api.Modules.Spaces.Repositories;
using AcademicSpace.Api.Shared.Errors;

namespace AcademicSpace.Api.Modules.Spaces.Services;

public class SpaceService(ISpaceRepository spaces)
{
    public async Task<List<SpaceDto>> GetAllAsync(bool includeInactive, TipoEspacio? tipo = null)
    {
        var list = await spaces.ListAsync(includeInactive, tipo);
        return list.Select(ToDto).ToList();
    }

    public async Task<SpaceDto> GetByIdAsync(Guid id, bool includeInactive)
    {
        var space = await spaces.GetByIdAsync(id, tracking: false);
        if (space is null || (!space.Activo && !includeInactive))
        {
            throw ApiException.NotFound("El espacio no fue encontrado.");
        }
        return ToDto(space);
    }

    public async Task<SpaceDto> CreateAsync(CreateSpaceRequest request)
    {
        Validate(request.Tipo, request.Capacidad);
        var nombre = request.Nombre.Trim();
        var ubicacion = request.Ubicacion.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(ubicacion))
        {
            throw ApiException.BadRequest("El nombre y la ubicación son obligatorios.");
        }
        if (await spaces.NameExistsAsync(nombre))
        {
            throw ApiException.Conflict("El nombre del espacio ya está en uso.");
        }

        var space = new Espacio
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Tipo = request.Tipo,
            Ubicacion = ubicacion,
            Capacidad = request.Capacidad,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow
        };
        await spaces.AddAsync(space);
        await spaces.SaveChangesAsync();
        return ToDto(space);
    }

    public async Task<SpaceDto> UpdateAsync(Guid id, UpdateSpaceRequest request)
    {
        Validate(request.Tipo, request.Capacidad);
        var space = await spaces.GetByIdAsync(id)
                    ?? throw ApiException.NotFound("El espacio no fue encontrado.");

        var nombre = request.Nombre.Trim();
        var ubicacion = request.Ubicacion.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(ubicacion))
        {
            throw ApiException.BadRequest("El nombre y la ubicación son obligatorios.");
        }
        if (await spaces.NameExistsAsync(nombre, id))
        {
            throw ApiException.Conflict("El nombre del espacio ya está en uso.");
        }

        space.Nombre = nombre;
        space.Tipo = request.Tipo;
        space.Ubicacion = ubicacion;
        space.Capacidad = request.Capacidad;
        space.Activo = request.Activo;
        space.FechaActualizacion = DateTime.UtcNow;
        await spaces.SaveChangesAsync();
        return ToDto(space);
    }

    private static void Validate(TipoEspacio tipo, int capacidad)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw ApiException.BadRequest("El tipo de espacio no es válido.");
        }
        if (capacidad <= 0)
        {
            throw ApiException.BadRequest("La capacidad debe ser mayor que cero.");
        }
    }

    private static SpaceDto ToDto(Espacio space) => new(
        space.Id,
        space.Nombre,
        space.Tipo.ToString(),
        space.Ubicacion,
        space.Capacidad,
        space.Activo);
}
