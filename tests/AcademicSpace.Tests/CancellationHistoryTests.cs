using System.Net;
using System.Net.Http.Json;
using AcademicSpace.Tests.TestSupport;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class CancellationHistoryTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CancelarSolicitudPendiente_RegistraFechaYEstado()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        var response = await solicitante.PostAsync($"/api/reservations/{reservationId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);
        Assert.Equal("Cancelada", json.GetProperty("estado").GetString());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, json.GetProperty("fechaCancelacion").ValueKind);
    }

    [Fact]
    public async Task CancelarReservaAprobada_LiberaLaFranja()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();
        var reservationId = await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "10:00:00");
        await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);

        var cancel = await solicitante.PostAsync($"/api/reservations/{reservationId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var availability = await ReadJsonAsync(await solicitante.GetAsync(
            $"/api/availability/spaces/{spaceId}?fecha={fecha}&horaInicio=08:00&horaFin=10:00"));
        Assert.True(availability.GetProperty("disponible").GetBoolean());

        await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "10:00:00");
    }

    [Fact]
    public async Task NoSePuedeCancelarUnaSolicitudAjena()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var otherEmail = "dueno@test.local";
        await CreateUserAsync(admin, otherEmail);
        var other = await ClientForAsync(otherEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(other, spaceId);

        var response = await solicitante.PostAsync($"/api/reservations/{reservationId}/cancel", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NoSePuedeCancelarUnaSolicitudRechazada()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        await aprobador.PostAsJsonAsync($"/api/reservations/{reservationId}/reject", new { motivo = "No procede" });

        var response = await solicitante.PostAsync($"/api/reservations/{reservationId}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task NoSePuedeCancelarDosVeces()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        var first = await solicitante.PostAsync($"/api/reservations/{reservationId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await solicitante.PostAsync($"/api/reservations/{reservationId}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task HistorialDelSolicitante_IncluyeTodasSusSolicitudesEnOrdenDescendente()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);

        var first = await CreateReservationAsync(solicitante, spaceId, FutureDate(10), "08:00:00", "09:00:00");
        await Task.Delay(50);
        var second = await CreateReservationAsync(solicitante, spaceId, FutureDate(11), "08:00:00", "09:00:00");
        await Task.Delay(50);
        var third = await CreateReservationAsync(solicitante, spaceId, FutureDate(12), "08:00:00", "09:00:00");

        await aprobador.PostAsync($"/api/reservations/{first}/approve", null);
        await solicitante.PostAsync($"/api/reservations/{second}/cancel", null);

        var history = await ReadJsonAsync(await solicitante.GetAsync("/api/reservations/history"));
        Assert.Equal(3, history.GetArrayLength());
        Assert.Equal(third, history[0].GetProperty("id").GetGuid());
        Assert.Equal(second, history[1].GetProperty("id").GetGuid());
        Assert.Equal(first, history[2].GetProperty("id").GetGuid());

        Assert.Equal("Aprobada", history[2].GetProperty("estado").GetString());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, history[2].GetProperty("fechaDecision").ValueKind);
        Assert.Equal("Cancelada", history[1].GetProperty("estado").GetString());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, history[1].GetProperty("fechaCancelacion").ValueKind);
        Assert.Equal("Pendiente", history[0].GetProperty("estado").GetString());
    }

    [Fact]
    public async Task HistorialDelSolicitante_SinSolicitudes_DevuelveListaVacia()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        await CreateUserAsync(admin, "sinhistorial@test.local");
        var client = await ClientForAsync("sinhistorial@test.local");

        var history = await ReadJsonAsync(await client.GetAsync("/api/reservations/history"));
        Assert.Equal(0, history.GetArrayLength());
    }

    [Fact]
    public async Task HistorialDelAprobadorYDelAdministrador_IncluyenTodasLasSolicitudes()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var secondEmail = "segundo@test.local";
        await CreateUserAsync(admin, secondEmail);
        var second = await ClientForAsync(secondEmail);
        var spaceId = await CreateSpaceAsync(admin);

        await CreateReservationAsync(solicitante, spaceId, FutureDate(10));
        await CreateReservationAsync(second, spaceId, FutureDate(11));

        var asAprobador = await ReadJsonAsync(await aprobador.GetAsync("/api/reservations/history"));
        Assert.Equal(2, asAprobador.GetArrayLength());

        var asAdmin = await ReadJsonAsync(await admin.GetAsync("/api/reservations/history"));
        Assert.Equal(2, asAdmin.GetArrayLength());
    }

    [Fact]
    public async Task ReservasRechazadasYCanceladas_PermanecenConsultablesEnElHistorial()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);

        var rejected = await CreateReservationAsync(solicitante, spaceId, FutureDate(10), "08:00:00", "09:00:00");
        var cancelled = await CreateReservationAsync(solicitante, spaceId, FutureDate(11), "08:00:00", "09:00:00");

        await aprobador.PostAsJsonAsync($"/api/reservations/{rejected}/reject", new { motivo = "No procede" });
        await solicitante.PostAsync($"/api/reservations/{cancelled}/cancel", null);

        var rejectedDetail = await solicitante.GetAsync($"/api/reservations/{rejected}");
        Assert.Equal(HttpStatusCode.OK, rejectedDetail.StatusCode);

        var cancelledDetail = await solicitante.GetAsync($"/api/reservations/{cancelled}");
        Assert.Equal(HttpStatusCode.OK, cancelledDetail.StatusCode);

        var history = await ReadJsonAsync(await solicitante.GetAsync("/api/reservations/history"));
        Assert.Equal(2, history.GetArrayLength());
        Assert.Contains(history.EnumerateArray(), r => r.GetProperty("estado").GetString() == "Rechazada");
        Assert.Contains(history.EnumerateArray(), r => r.GetProperty("estado").GetString() == "Cancelada");
    }
}
