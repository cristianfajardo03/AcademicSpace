using System.ComponentModel.DataAnnotations;
using AcademicSpace.Api.Modules.Spaces.Entities;

namespace AcademicSpace.Api.Modules.Spaces.Contracts;

public record CreateSpaceRequest(
    [Required, StringLength(150)] string Nombre,
    TipoEspacio Tipo,
    [Required, StringLength(200)] string Ubicacion,
    [Range(1, int.MaxValue)] int Capacidad);

public record UpdateSpaceRequest(
    [Required, StringLength(150)] string Nombre,
    TipoEspacio Tipo,
    [Required, StringLength(200)] string Ubicacion,
    [Range(1, int.MaxValue)] int Capacidad,
    bool Activo);

public record SpaceDto(
    Guid Id,
    string Nombre,
    string Tipo,
    string Ubicacion,
    int Capacidad,
    bool Activo);
