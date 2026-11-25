using System.Net;
using System.Net.Http.Json;
using AcademicSpace.Api.Modules.Reservations.Entities;
using AcademicSpace.Api.Modules.Users.Entities;
using AcademicSpace.Api.Shared.Persistence;
using AcademicSpace.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class ApprovalTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Pendientes_SeOrdenanDeLaMasAntiguaALaMasReciente()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();

        var first = await CreateReservationAsync(solicitante, spaceId, fecha, "08:00:00", "09:00:00");
        var second = await CreateReservationAsync(solicitante, spaceId, fecha, "09:00:00", "10:00:00");

        var json = await ReadJsonAsync(await aprobador.GetAsync("/api/reservations/pending"));
        Assert.Equal(2, json.GetArrayLength());
        Assert.Equal(first, json[0].GetProperty("id").GetGuid());
        Assert.Equal(second, json[1].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Pendientes_SinSolicitudes_DevuelveListaVacia()
    {
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var json = await ReadJsonAsync(await aprobador.GetAsync("/api/reservations/pending"));
        Assert.Equal(0, json.GetArrayLength());
    }

    [Fact]
    public async Task AprobarSolicitud_RegistraAprobadorYFecha()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        var response = await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonAsync(response);
        Assert.Equal("Aprobada", json.GetProperty("estado").GetString());
        Assert.Equal("Aprobador de Pruebas", json.GetProperty("aprobadorNombre").GetString());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, json.GetProperty("fechaDecision").ValueKind);
    }

    [Fact]
    public async Task AprobarSolicitud_YaDecidida_Devuelve409()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        var first = await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Aprobar_ComoSolicitante_Devuelve403()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        var response = await solicitante.PostAsync($"/api/reservations/{reservationId}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AprobarSolicitud_ConConflictoSobrevenido_Devuelve409()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var fecha = DateOnly.FromDateTime(DateTime.Now.AddDays(40));

        var pendingId = await InsertConflictingReservationsAsync(spaceId, fecha);

        var response = await aprobador.PostAsync($"/api/reservations/{pendingId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var json = await ReadJsonAsync(await aprobador.GetAsync($"/api/reservations/{pendingId}"));
        Assert.Equal("Pendiente", json.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task RechazarSolicitud_RegistraMotivoAprobadorYFecha()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        var response = await aprobador.PostAsJsonAsync($"/api/reservations/{reservationId}/reject", new
        {
            motivo = "Documentación incompleta"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Rechazada", json.GetProperty("estado").GetString());
        Assert.Equal("Documentación incompleta", json.GetProperty("motivoRechazo").GetString());
        Assert.Equal("Aprobador de Pruebas", json.GetProperty("aprobadorNombre").GetString());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, json.GetProperty("fechaDecision").ValueKind);
    }

    [Fact]
    public async Task RechazarSolicitud_SinMotivo_Devuelve400()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        var response = await aprobador.PostAsJsonAsync($"/api/reservations/{reservationId}/reject", new
        {
            motivo = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RechazarSolicitud_YaDecidida_Devuelve409()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);

        var response = await aprobador.PostAsJsonAsync($"/api/reservations/{reservationId}/reject", new
        {
            motivo = "Tarde"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SolicitudAprobada_DejaDeEstarPendienteYConservaTrazabilidad()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);
        var spaceId = await CreateSpaceAsync(admin);
        var reservationId = await CreateReservationAsync(solicitante, spaceId);

        await aprobador.PostAsync($"/api/reservations/{reservationId}/approve", null);

        var pending = await ReadJsonAsync(await aprobador.GetAsync("/api/reservations/pending"));
        Assert.Equal(0, pending.GetArrayLength());

        var detail = await ReadJsonAsync(await solicitante.GetAsync($"/api/reservations/{reservationId}"));
        Assert.Equal("Aprobada", detail.GetProperty("estado").GetString());
        Assert.Equal("Aprobador de Pruebas", detail.GetProperty("aprobadorNombre").GetString());

        var history = await ReadJsonAsync(await solicitante.GetAsync("/api/reservations/history"));
        Assert.Single(history.EnumerateArray());
        Assert.Equal("Aprobada", history[0].GetProperty("estado").GetString());
    }

    private async Task<Guid> InsertConflictingReservationsAsync(Guid spaceId, DateOnly fecha)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademicSpaceDbContext>();
        var solicitante = await db.Usuarios.FirstAsync(u => u.Correo == ApiFactory.SolicitanteEmail);

        var approved = new Reserva
        {
            Id = Guid.NewGuid(),
            EspacioId = spaceId,
            SolicitanteId = solicitante.Id,
            Fecha = fecha,
            HoraInicio = new TimeOnly(9, 0),
            HoraFin = new TimeOnly(11, 0),
            Motivo = "Reserva ya aprobada",
            Estado = EstadoReserva.Aprobada,
            FechaSolicitud = DateTime.UtcNow.AddMinutes(-2),
            FechaDecision = DateTime.UtcNow.AddMinutes(-1),
            Version = 1
        };
        var pending = new Reserva
        {
            Id = Guid.NewGuid(),
            EspacioId = spaceId,
            SolicitanteId = solicitante.Id,
            Fecha = fecha,
            HoraInicio = new TimeOnly(8, 0),
            HoraFin = new TimeOnly(10, 0),
            Motivo = "Solicitud pendiente en conflicto",
            Estado = EstadoReserva.Pendiente,
            FechaSolicitud = DateTime.UtcNow.AddMinutes(-1),
            Version = 1
        };

        db.Reservas.AddRange(approved, pending);
        await db.SaveChangesAsync();
        return pending.Id;
    }
}
