namespace AcademicSpace.Api.Shared.Auth;

public static class SystemRoles
{
    public const string Administrador = "Administrador";
    public const string Solicitante = "Solicitante";
    public const string Aprobador = "Aprobador";

    public static readonly string[] All = [Administrador, Solicitante, Aprobador];

    public static int? GetId(string? nombre) => nombre switch
    {
        Administrador => 1,
        Solicitante => 2,
        Aprobador => 3,
        _ => null
    };

    public static bool IsValid(string? nombre) => GetId(nombre) is not null;
}
