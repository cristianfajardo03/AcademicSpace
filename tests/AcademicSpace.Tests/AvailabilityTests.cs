using System.Net;
using System.Net.Http.Json;
using AcademicSpace.Tests.TestSupport;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class AvailabilityTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    private static string AvailabilityUrl(Guid spaceId, string fecha, string inicio, string fin) =>
        $"/api/availability/spaces/{spaceId}?fecha={fecha}&horaInicio={inicio}&horaFin={fin}";

    private async Task<(HttpClient Solicitante, HttpClient Aprobador)> RolesAsync()
    {
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        return (solicitante, aprobador);
    }

    [Fact]
    public async Task Disponibilidad_ExponeFranjasOcupadasSinDatosDelSolicitante()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var (solicitante, aprobador) = await RolesAsync();
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();
        var reservationId = await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "10:00:00");
        var approve = await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var response = await solicitante.GetAsync(AvailabilityUrl(spaceId, fecha, "09:00", "11:00"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);
        Assert.False(json.GetProperty("disponible").GetBoolean());
        var ocupaciones = json.GetProperty("ocupaciones");
        Assert.Equal(1, ocupaciones.GetArrayLength());
        Assert.Equal("08:00:00", ocupaciones[0].GetProperty("horaInicio").GetString());
        Assert.Equal("10:00:00", ocupaciones[0].GetProperty("horaFin").GetString());
        Assert.False(json.TryGetProperty("solicitante", out _));
        Assert.False(json.TryGetProperty("solicitanteNombre", out _));
    }

    [Theory]
    [InlineData("09:00", "11:00")]
    [InlineData("07:00", "11:00")]
    [InlineData("08:00", "10:00")]
    public async Task Disponibilidad_ConSuperposicion_NoEstaDisponible(string inicio, string fin)
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var (solicitante, aprobador) = await RolesAsync();
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();

        var reservationId = await CreateReservationAsync(
            solicitante, spaceId, fecha, "08:00:00", "10:00:00");

        var approve = await aprobador.PostAsync(
            $"/api/reservations/{reservationId}/approve", null);

        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var response = await solicitante.GetAsync(
            AvailabilityUrl(spaceId, fecha, inicio, fin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);

        Assert.False(json.GetProperty("disponible").GetBoolean());
    }

    [Theory]
    [InlineData("10:00", "12:00")]
    [InlineData("06:00", "08:00")]
    public async Task Disponibilidad_SinSuperposicion_EstaDisponible(string inicio, string fin)
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var (solicitante, aprobador) = await RolesAsync();
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();
        var reservationId = await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "10:00:00");
        await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);

        var json = await ReadJsonAsync(
            await solicitante.GetAsync(AvailabilityUrl(spaceId, fecha, inicio, fin)));

        Assert.True(json.GetProperty("disponible").GetBoolean());
        Assert.Equal(0, json.GetProperty("ocupaciones").GetArrayLength());
    }

    [Fact]
    public async Task EspaciosDisponibles_ExcluyeLosOcupados()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var (solicitante, aprobador) = await RolesAsync();
        var ocupadoId = await CreateSpaceAsync(admin, "Laboratorio Ocupado", tipo: 1);
        var libreId = await CreateSpaceAsync(admin, "Auditorio Libre", tipo: 2);
        var fecha = FutureDate();

        var reservationId = await CreateReservationAsync(solicitante, ocupadoId, fecha, "08:00:00", "10:00:00");
        await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);

        var json = await ReadJsonAsync(
            await solicitante.GetAsync($"/api/availability/spaces?fecha={fecha}&horaInicio=08:00&horaFin=10:00"));

        var espacios = json.GetProperty("espacios");
        Assert.Single(espacios.EnumerateArray());
        Assert.Equal(libreId, espacios[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task EspaciosDisponibles_FiltraPorTipo()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        await CreateSpaceAsync(admin, "Laboratorio Uno", tipo: 1);
        await CreateSpaceAsync(admin, "Auditorio Dos", tipo: 2);
        var fecha = FutureDate();

        var laboratorios = await ReadJsonAsync(await solicitante.GetAsync(
            $"/api/availability/spaces?fecha={fecha}&horaInicio=08:00&horaFin=10:00&tipo=1"));
        Assert.Single(laboratorios.GetProperty("espacios").EnumerateArray());
        Assert.Equal("Laboratorio", laboratorios.GetProperty("espacios")[0].GetProperty("tipo").GetString());

        var salas = await ReadJsonAsync(await solicitante.GetAsync(
            $"/api/availability/spaces?fecha={fecha}&horaInicio=08:00&horaFin=10:00&tipo=3"));
        Assert.Equal(0, salas.GetProperty("espacios").GetArrayLength());
        Assert.Contains("No hay espacios", salas.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task EspaciosDisponibles_CuandoTodosEstanOcupados_DevuelveListaVacia()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var (solicitante, aprobador) = await RolesAsync();
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();
        var reservationId = await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "10:00:00");
        await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);

        var json = await ReadJsonAsync(
            await solicitante.GetAsync($"/api/availability/spaces?fecha={fecha}&horaInicio=09:00&horaFin=11:00"));

        Assert.Equal(0, json.GetProperty("espacios").GetArrayLength());
    }

    [Fact]
    public async Task Disponibilidad_ConFechaPasada_Devuelve400()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)).ToString("yyyy-MM-dd");

        var response = await solicitante.GetAsync(
            $"/api/availability/spaces?fecha={fecha}&horaInicio=08:00&horaFin=10:00");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Disponibilidad_ConHoraFinAnteriorAlInicio_Devuelve400()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();

        var response = await solicitante.GetAsync(AvailabilityUrl(spaceId, fecha, "11:00", "09:00"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReservasRechazadas_NoOcupanFranjas()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var (solicitante, aprobador) = await RolesAsync();
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();
        var reservationId = await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "10:00:00");

        var reject = await aprobador.PostAsJsonAsync($"/api/reservations/{reservationId}/reject", new
        {
            motivo = "No cumple requisitos"
        });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

        var json = await ReadJsonAsync(await solicitante.GetAsync(AvailabilityUrl(spaceId, fecha, "08:00", "10:00")));
        Assert.True(json.GetProperty("disponible").GetBoolean());
    }

    [Fact]
    public async Task ReservasCanceladas_NoOcupanFranjas()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var (solicitante, aprobador) = await RolesAsync();
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();
        var reservationId = await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "10:00:00");
        await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);

        var cancel = await solicitante.PostAsync($"/api/reservations/{reservationId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var json = await ReadJsonAsync(await solicitante.GetAsync(AvailabilityUrl(spaceId, fecha, "08:00", "10:00")));
        Assert.True(json.GetProperty("disponible").GetBoolean());
    }

    [Fact]
    public async Task EspaciosInactivos_NuncaAparecenComoDisponibles()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin, "Sala Inactiva");
        await admin.PutAsJsonAsync($"/api/spaces/{spaceId}", new
        {
            nombre = "Sala Inactiva",
            tipo = 1,
            ubicacion = "Bloque 1",
            capacidad = 30,
            activo = false
        });

        var fecha = FutureDate();
        var list = await ReadJsonAsync(await solicitante.GetAsync(
            $"/api/availability/spaces?fecha={fecha}&horaInicio=08:00&horaFin=10:00"));
        Assert.Equal(0, list.GetProperty("espacios").GetArrayLength());

        var detail = await ReadJsonAsync(await solicitante.GetAsync(AvailabilityUrl(spaceId, fecha, "08:00", "10:00")));
        Assert.False(detail.GetProperty("disponible").GetBoolean());
    }
}
