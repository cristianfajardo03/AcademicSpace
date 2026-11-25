using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademicSpace.Tests.TestSupport;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class AuthTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_ConCredencialesValidas_DevuelveTokenYUsuario()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            correo = ApiFactory.AdminEmail,
            contrasena = ApiFactory.Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("token").GetString()));
        Assert.Equal("Administrador", json.GetProperty("usuario").GetProperty("rol").GetString());
        Assert.False(json.GetProperty("usuario").TryGetProperty("hashContrasena", out _));
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_Devuelve401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            correo = ApiFactory.AdminEmail,
            contrasena = "ClaveIncorrecta1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ConCuentaInactiva_Devuelve401()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var userId = await CreateUserAsync(admin, "inactivo@test.local");

        var update = await admin.PutAsJsonAsync($"/api/users/{userId}", new
        {
            nombre = "Usuario de Pruebas",
            correo = "inactivo@test.local",
            rol = "Solicitante",
            activo = false
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            correo = "inactivo@test.local",
            contrasena = ApiFactory.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevocaLaSesionYLasPeticionesPosterioresDevuelven401()
    {
        var token = await LoginAsync(ApiFactory.AdminEmail);
        var client = AuthenticatedClient(token);

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var afterLogout = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_SinToken_Devuelve401()
    {
        var response = await Client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointDeAdministrador_ConRolSolicitante_Devuelve403()
    {
        var client = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var response = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DesactivarUsuario_RevocaSusSesionesActivas()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var userId = await CreateUserAsync(admin, "revocado@test.local");
        var token = await LoginAsync("revocado@test.local");
        var client = AuthenticatedClient(token);

        var before = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        var update = await admin.PutAsJsonAsync($"/api/users/{userId}", new
        {
            nombre = "Usuario de Pruebas",
            correo = "revocado@test.local",
            rol = "Solicitante",
            activo = false
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var after = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task Login_ConCorreoInexistente_Devuelve401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            correo = "nadie@test.local",
            contrasena = ApiFactory.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ConDatosInvalidos_Devuelve400ConProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            correo = "no-es-correo",
            contrasena = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(400, json.GetProperty("status").GetInt32());
        Assert.True(json.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("correo", out var correoErrors)
                    || errors.TryGetProperty("Correo", out correoErrors));
        Assert.True(correoErrors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task TokenManipulado_Devuelve401()
    {
        var client = AuthenticatedClient("token.invalido.firmado");
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
