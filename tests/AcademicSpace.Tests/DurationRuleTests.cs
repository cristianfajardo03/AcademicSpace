using System.Net;
using System.Net.Http.Json;
using AcademicSpace.Api.Modules.Reservations.Entities;
using AcademicSpace.Api.Shared.Persistence;
using AcademicSpace.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class DurationRuleTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ReservaDeTresHoras_EsPermitida()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);

        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha = FutureDate(),
            horaInicio = "08:00:00",
            horaFin = "11:00:00",
            motivo = "Reserva de tres horas"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ReservaDeExactamenteCuatroHoras_EsPermitida()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);

        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha = FutureDate(),
            horaInicio = "08:00:00",
            horaFin = "12:00:00",
            motivo = "Reserva de cuatro horas"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ReservaDeMasDeCuatroHoras_EsRechazada()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);

        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha = FutureDate(),
            horaInicio = "08:00:00",
            horaFin = "12:01:00",
            motivo = "Reserva de cuatro horas y un minuto"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReservaDeSeisHoras_EsRechazada()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var spaceId = await CreateSpaceAsync(admin);

        var response = await solicitante.PostAsJsonAsync("/api/reservations", new
        {
            espacioId = spaceId,
            fecha = FutureDate(),
            horaInicio = "08:00:00",
            horaFin = "14:00:00",
            motivo = "Reserva de seis horas"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AprobarReservaHeredadaDeSeisHoras_EsRechazadaYContinuaPendiente()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var aprobador = await ClientForAsync(ApiFactory.AprobadorEmail);

        var spaceId = await CreateSpaceAsync(admin);

        Guid reservaId;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademicSpaceDbContext>();

            var solicitante = await db.Usuarios
                .SingleAsync(u => u.Correo == ApiFactory.SolicitanteEmail);

            var reserva = new Reserva
            {
                Id = Guid.NewGuid(),
                EspacioId = spaceId,
                SolicitanteId = solicitante.Id,
                Fecha = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(14, 0),
                Motivo = "Reserva heredada de seis horas",
                Estado = EstadoReserva.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                Version = 1
            };

            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();

            reservaId = reserva.Id;
        }

        var approveResponse = await aprobador.PostAsync(
            $"/api/reservations/{reservaId}/approve", null);

        Assert.Equal(HttpStatusCode.Conflict, approveResponse.StatusCode);

        var getResponse = await aprobador.GetAsync(
            $"/api/reservations/{reservaId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var json = await ReadJsonAsync(getResponse);

        Assert.Equal(
            EstadoReserva.Pendiente.ToString(),
            json.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task ConsultarEspaciosDisponibles_ExactamenteCuatroHoras_EsPermitido()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);

        await CreateSpaceAsync(admin);

        var fecha = FutureDate();

        var response = await solicitante.GetAsync(
            $"/api/availability/spaces?fecha={fecha}&horaInicio=08:00:00&horaFin=12:00:00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConsultarEspaciosDisponibles_MasDeCuatroHoras_EsRechazado()
    {
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var fecha = FutureDate();

        var response = await solicitante.GetAsync(
            $"/api/availability/spaces?fecha={fecha}&horaInicio=08:00:00&horaFin=12:01:00");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConsultarDisponibilidadDeEspacio_ExactamenteCuatroHoras_EsPermitido()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);

        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();

        var response = await solicitante.GetAsync(
            $"/api/availability/spaces/{spaceId}?fecha={fecha}&horaInicio=08:00:00&horaFin=12:00:00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConsultarDisponibilidadDeEspacio_MasDeCuatroHoras_EsRechazado()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);

        var spaceId = await CreateSpaceAsync(admin);
        var fecha = FutureDate();

        var response = await solicitante.GetAsync(
            $"/api/availability/spaces/{spaceId}?fecha={fecha}&horaInicio=08:00:00&horaFin=12:01:00");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
