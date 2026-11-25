using System.Net;
using System.Net.Http.Json;
using AcademicSpace.Tests.TestSupport;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class SpaceTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task RegistrarEspacio_Valido_Devuelve201YActivo()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);

        var response = await admin.PostAsJsonAsync("/api/spaces", new
        {
            nombre = "Auditorio Principal",
            tipo = 2,
            ubicacion = "Bloque A",
            capacidad = 120
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Auditorio Principal", json.GetProperty("nombre").GetString());
        Assert.Equal("Auditorio", json.GetProperty("tipo").GetString());
        Assert.True(json.GetProperty("activo").GetBoolean());
        Assert.Equal(120, json.GetProperty("capacidad").GetInt32());
    }

    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RegistrarEspacio_ConTipoInvalido_Devuelve400(int tipo)
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var response = await admin.PostAsJsonAsync("/api/spaces", new
        {
            nombre = "Espacio Inválido",
            tipo,
            ubicacion = "Bloque A",
            capacidad = 10
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarEspacio_ConNombreDuplicado_Devuelve409()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        await CreateSpaceAsync(admin, "Sala Duplicada");

        var response = await admin.PostAsJsonAsync("/api/spaces", new
        {
            nombre = "Sala Duplicada",
            tipo = 3,
            ubicacion = "Bloque B",
            capacidad = 20
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task RegistrarEspacio_ConCapacidadInvalida_Devuelve400(int capacidad)
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var response = await admin.PostAsJsonAsync("/api/spaces", new
        {
            nombre = "Capacidad Inválida",
            tipo = 1,
            ubicacion = "Bloque A",
            capacidad
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarEspacio_ComoSolicitante_Devuelve403()
    {
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var response = await solicitante.PostAsJsonAsync("/api/spaces", new
        {
            nombre = "No Permitido",
            tipo = 1,
            ubicacion = "Bloque A",
            capacidad = 10
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConsultarEspacios_UsuariosNoAdministradoresSoloVenActivos()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var activoId = await CreateSpaceAsync(admin, "Laboratorio Activo");
        var inactivoId = await CreateSpaceAsync(admin, "Laboratorio Inactivo");

        var update = await admin.PutAsJsonAsync($"/api/spaces/{inactivoId}", new
        {
            nombre = "Laboratorio Inactivo",
            tipo = 1,
            ubicacion = "Bloque 1",
            capacidad = 30,
            activo = false
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var list = await ReadJsonAsync(await solicitante.GetAsync("/api/spaces"));
        Assert.Single(list.EnumerateArray());
        Assert.Equal(activoId, list.EnumerateArray().First().GetProperty("id").GetGuid());

        var adminList = await ReadJsonAsync(await admin.GetAsync("/api/spaces"));
        Assert.Equal(2, adminList.GetArrayLength());
    }

    [Fact]
    public async Task ConsultarDetalle_DeEspacioInactivo_Devuelve404ParaNoAdministrador()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var id = await CreateSpaceAsync(admin, "Sala Ocultable");
        await admin.PutAsJsonAsync($"/api/spaces/{id}", new
        {
            nombre = "Sala Ocultable",
            tipo = 1,
            ubicacion = "Bloque 1",
            capacidad = 30,
            activo = false
        });

        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var response = await solicitante.GetAsync($"/api/spaces/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var adminResponse = await admin.GetAsync($"/api/spaces/{id}");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task AdministradorPuedeModificarTodosLosCampos()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var id = await CreateSpaceAsync(admin, "Sala Original");

        var response = await admin.PutAsJsonAsync($"/api/spaces/{id}", new
        {
            nombre = "Sala Modificada",
            tipo = 3,
            ubicacion = "Bloque C",
            capacidad = 45,
            activo = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Sala Modificada", json.GetProperty("nombre").GetString());
        Assert.Equal("SalaComputo", json.GetProperty("tipo").GetString());
        Assert.Equal("Bloque C", json.GetProperty("ubicacion").GetString());
        Assert.Equal(45, json.GetProperty("capacidad").GetInt32());
    }

    [Fact]
    public async Task ModificarEspacio_ConNombreDuplicado_Devuelve409()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        await CreateSpaceAsync(admin, "Primera Sala");
        var id = await CreateSpaceAsync(admin, "Segunda Sala");

        var response = await admin.PutAsJsonAsync($"/api/spaces/{id}", new
        {
            nombre = "Primera Sala",
            tipo = 1,
            ubicacion = "Bloque 1",
            capacidad = 30,
            activo = true
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task NoExisteEndpointDeEliminacionFisica()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var id = await CreateSpaceAsync(admin, "Sala Sin Borrado");

        var response = await admin.DeleteAsync($"/api/spaces/{id}");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

        var stillThere = await admin.GetAsync($"/api/spaces/{id}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }
}
