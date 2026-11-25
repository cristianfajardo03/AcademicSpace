using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AcademicSpace.Tests.TestSupport;

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFactory>;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected ApiFactory Factory { get; }
    protected HttpClient Client { get; }

    protected IntegrationTestBase(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public Task InitializeAsync() => Factory.ResetAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<string> LoginAsync(string email, string password = ApiFactory.Password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { correo = email, contrasena = password });
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString()!;
    }

    protected HttpClient AuthenticatedClient(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected async Task<HttpClient> ClientForAsync(string email)
    {
        var token = await LoginAsync(email);
        return AuthenticatedClient(token);
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    protected static async Task<Guid> CreateSpaceAsync(
        HttpClient admin,
        string nombre = "Laboratorio A",
        int tipo = 1,
        string ubicacion = "Bloque 1",
        int capacidad = 30)
    {
        var response = await admin.PostAsJsonAsync("/api/spaces", new
        {
            nombre,
            tipo,
            ubicacion,
            capacidad
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        return json.GetProperty("id").GetGuid();
    }

    protected static async Task<Guid> CreateReservationAsync(
        HttpClient solicitante,
        Guid espacioId,
        string fecha = "2027-06-01",
        string horaInicio = "08:00:00",
        string horaFin = "10:00:00",
        string motivo = "Clase de prueba")
    {
        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId,
            fecha,
            horaInicio,
            horaFin,
            motivo
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        return json.GetProperty("id").GetGuid();
    }

    protected static string FutureDate(int daysFromNow = 30) =>
        DateOnly.FromDateTime(DateTime.Now.AddDays(daysFromNow)).ToString("yyyy-MM-dd");

    protected static async Task<Guid> CreateUserAsync(
        HttpClient admin,
        string correo,
        string rol = "Solicitante",
        string nombre = "Usuario de Pruebas",
        string contrasena = ApiFactory.Password)
    {
        var response = await admin.PostAsJsonAsync("/api/users", new
        {
            nombre,
            correo,
            contrasena,
            rol
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        return json.GetProperty("id").GetGuid();
    }
}
