using AcademicSpace.Api.Modules.Spaces.Contracts;

namespace AcademicSpace.Api.Modules.Availability.Contracts;

public record OccupiedIntervalDto(TimeOnly HoraInicio, TimeOnly HoraFin);

public record SpaceAvailabilityDto(
    Guid EspacioId,
    string EspacioNombre,
    bool Disponible,
    IReadOnlyList<OccupiedIntervalDto> Ocupaciones);

public record AvailableSpacesResponse(IReadOnlyList<SpaceDto> Espacios, string Mensaje);
