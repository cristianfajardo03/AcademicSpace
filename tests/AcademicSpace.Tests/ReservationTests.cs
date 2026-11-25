using System.Net;
using System.Net.Http.Json;
using AcademicSpace.Api.Modules.Reservations.Services;
using AcademicSpace.Tests.TestSupport;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class ReservationTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CrearSolicitud_Valida_Devuelve201PendienteConPropietario()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();

        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha,
            horaInicio = "08:00:00",
            horaFin = "10:00:00",
            motivo = "Clase de algoritmos"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Pendiente", json.GetProperty("estado").GetString());
        Assert.Equal("Solicitante de Pruebas", json.GetProperty("solicitanteNombre").GetString());
        Assert.Equal("Clase de algoritmos", json.GetProperty("motivo").GetString());
        Assert.Equal(spaceId, json.GetProperty("espacioId").GetGuid());
    }

    [Theory]
    [InlineData("2020-01-01", "08:00:00", "10:00:00", "Motivo válido")]
    [InlineData("FUTURE", "10:00:00", "09:00:00", "Motivo válido")]
    [InlineData("FUTURE", "08:00:00", "08:00:00", "Motivo válido")]
    [InlineData("FUTURE", "08:00:00", "10:00:00", "")]
    public async Task CrearSolicitud_ConDatosInvalidos_Devuelve400(
        string fecha, string inicio, string fin, string motivo)
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        if (fecha == "FUTURE")
        {
            fecha = FutureDate();
        }

        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha,
            horaInicio = inicio,
            horaFin = fin,
            motivo
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CrearSolicitud_ParaEspacioInactivo_Devuelve409SinCrearRegistro()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin, "Sala Desactivada");
        await admin.PutAsJsonAsync($"/api/spaces/{spaceId}", new
        {
            nombre = "Sala Desactivada",
            tipo = 1,
            ubicacion = "Bloque 1",
            capacidad = 30,
            activo = false
        });

        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha = FutureDate(),
            horaInicio = "08:00:00",
            horaFin = "10:00:00",
            motivo = "Intento inválido"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var list = await ReadJsonAsync(await solicitante.GetAsync("/api/reservations"));
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task CrearSolicitud_EnFranjaOcupada_Devuelve409SinCrearRegistro()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();
        await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "10:00:00");

        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha,
            horaInicio = "09:00:00",
            horaFin = "11:00:00",
            motivo = "Franja ocupada"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var list = await ReadJsonAsync(await solicitante.GetAsync("/api/reservations"));
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task DosEspaciosDistintos_PuedenTenerReservasEnLaMismaFranja()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var first = await CreateSpaceAsync(admin, "Sala Uno");
        var second = await CreateSpaceAsync(admin, "Sala Dos");
        var fecha = FutureDate();

        await CreateReservationAsync(solicitante, first, fecha);
        await CreateReservationAsync(solicitante, second, fecha);
    }

    [Theory]
    [InlineData("08:00", "10:00", "09:00", "11:00", true)]
    [InlineData("08:00", "10:00", "07:00", "12:00", true)]
    [InlineData("08:00", "10:00", "10:00", "12:00", false)]
    [InlineData("08:00", "10:00", "06:00", "08:00", false)]
    [InlineData("08:00", "10:00", "08:00", "10:00", true)]
    public void ReglaDeSuperposicion_UsaIntervalosSemiabiertos(
        string inicio, string fin, string otroInicio, string otroFin, bool esperado)
    {
        var fecha = new DateOnly(2027, 6, 1);
        var result = ReservationRules.Overlaps(
            fecha, TimeOnly.Parse(inicio), TimeOnly.Parse(fin),
            fecha, TimeOnly.Parse(otroInicio), TimeOnly.Parse(otroFin));

        Assert.Equal(esperado, result);
    }

    [Fact]
    public void ReglaDeSuperposicion_ConFechasDistintas_NoHayConflicto()
    {
        var result = ReservationRules.Overlaps(
            new DateOnly(2027, 6, 1), new TimeOnly(8, 0), new TimeOnly(10, 0),
            new DateOnly(2027, 6, 2), new TimeOnly(8, 0), new TimeOnly(10, 0));

        Assert.False(result);
    }

    [Fact]
    public async Task DosSolicitudesSimultaneasSuperpuestas_SoloRegistranUna()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();

        var requests = Enumerable.Range(0, 2).Select(index =>
            solicitante.PostAsJsonAsync("/api/reservations", new
            {
                espacioId = spaceId,
                fecha,
                horaInicio = "08:00:00",
                horaFin = "10:00:00",
                motivo = $"Solicitud concurrente {index}"
            })).ToList();

        var responses = await Task.WhenAll(requests);
        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflicts = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, created);
        Assert.Equal(1, conflicts);

        var list = await ReadJsonAsync(await solicitante.GetAsync("/api/reservations"));
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task SolicitanteSoloVeSusPropiasSolicitudes()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var first = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var secondEmail = "otro@test.local";
        await CreateUserAsync(admin, secondEmail);
        var second = await ClientForAsync(secondEmail);
        var spaceId = await CreateSpaceAsync(admin);

        await CreateReservationAsync(first, spaceId, FutureDate(10), "08:00:00", "10:00:00");
        await CreateReservationAsync(second, spaceId, FutureDate(11), "08:00:00", "10:00:00");

        var firstList = await ReadJsonAsync(await first.GetAsync("/api/reservations"));
        Assert.Single(firstList.EnumerateArray());
        Assert.Equal("Solicitante de Pruebas", firstList[0].GetProperty("solicitanteNombre").GetString());
    }

    [Fact]
    public async Task AprobadorYAdministradorVenTodasLasSolicitudes()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        await CreateReservationAsync(solicitante, spaceId);

        var asAprobador = await ReadJsonAsync(await aprobador.GetAsync("/api/reservations"));
        Assert.Single(asAprobador.EnumerateArray());

        var asAdmin = await ReadJsonAsync(await admin.GetAsync("/api/reservations"));
        Assert.Single(asAdmin.EnumerateArray());
    }

    [Fact]
    public async Task SolicitanteNoPuedeConsultarDetalleAjeno()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var otherEmail = "ajeno@test.local";
        await CreateUserAsync(admin, otherEmail);
        var other = await ClientForAsync(otherEmail);
        var spaceId = await CreateSpaceAsync(admin);

        var reservationId = await CreateReservationAsync(other, spaceId);

        var response = await solicitante.GetAsync($"/api/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CrearSolicitud_ComoAprobador_Devuelve403()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);

        var response = await aprobador.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha = FutureDate(),
            horaInicio = "08:00:00",
            horaFin = "10:00:00",
            motivo = "No permitido"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReservasDeEspacioDesactivado_ConservanSuEstadoYSeConsultaLos()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin, "Sala Que Se Desactiva");
        var reservationId = await CreateReservationAsync(solicitante, spaceId);
        await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);

        await admin.PutAsJsonAsync($"/api/spaces/{spaceId}", new
        {
            nombre = "Sala Que Se Desactiva",
            tipo = 1,
            ubicacion = "Bloque 1",
            capacidad = 30,
            activo = false
        });

        var response = await solicitante.GetAsync($"/api/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Aprobada", json.GetProperty("estado").GetString());
        Assert.Equal(spaceId, json.GetProperty("espacioId").GetGuid());
    }
}
