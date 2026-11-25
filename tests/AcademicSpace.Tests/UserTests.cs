using System.Net;
using System.Net.Http.Json;
using AcademicSpace.Tests.TestSupport;
using Xunit;

namespace AcademicSpace.Tests;

[Collection("api")]
public class UserTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task RegistrarUsuario_Valido_Devuelve201YApareceEnElListado()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);

        var response = await admin.PostAsJsonAsync("/api/users", new
        {
            nombre = "Ana Torres",
            correo = "ana@test.local",
            contrasena = "Password123!",
            rol = "Solicitante"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("ana@test.local", json.GetProperty("correo").GetString());
        Assert.Equal("Solicitante", json.GetProperty("rol").GetString());
        Assert.True(json.GetProperty("activo").GetBoolean());
        Assert.False(json.TryGetProperty("hashContrasena", out _));

        var list = await admin.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var users = await ReadJsonAsync(list);
        Assert.Contains(users.EnumerateArray(), u => u.GetProperty("correo").GetString() == "ana@test.local");
    }

    [Fact]
    public async Task RegistrarUsuario_ConCorreoDuplicado_Devuelve409()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        await CreateUserAsync(admin, "duplicado@test.local");

        var response = await admin.PostAsJsonAsync("/api/users", new
        {
            nombre = "Otro Nombre",
            correo = "duplicado@test.local",
            contrasena = "Password123!",
            rol = "Aprobador"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("", "correo@test.local", "Password123!", "Solicitante")]
    [InlineData("Nombre", "correo-invalido", "Password123!", "Solicitante")]
    [InlineData("Nombre", "corto@test.local", "corta", "Solicitante")]
    [InlineData("Nombre", "rol@test.local", "Password123!", "Superusuario")]
    public async Task RegistrarUsuario_ConDatosInvalidos_Devuelve400(
        string nombre, string correo, string contrasena, string rol)
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);

        var response = await admin.PostAsJsonAsync("/api/users", new
        {
            nombre,
            correo,
            contrasena,
            rol
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConsultarUsuarios_ComoAdministrador_DevuelveListaSinContrasenas()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var response = await admin.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await ReadJsonAsync(response);
        Assert.Equal(3, users.GetArrayLength());
        foreach (var user in users.EnumerateArray())
        {
            Assert.False(user.TryGetProperty("hashContrasena", out _));
            Assert.False(user.TryGetProperty("contrasena", out _));
        }
    }

    [Fact]
    public async Task ConsultarPerfilPropio_DevuelveElUsuarioAutenticado()
    {
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var response = await solicitante.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(ApiFactory.SolicitanteEmail, json.GetProperty("correo").GetString());
        Assert.Equal("Solicitante", json.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task ConsultarDetalleDeOtroUsuario_ComoSolicitante_Devuelve403()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var list = await admin.GetAsync("/api/users");
        var users = await ReadJsonAsync(list);
        var otherId = users.EnumerateArray()
            .First(u => u.GetProperty("correo").GetString() == ApiFactory.AprobadorEmail)
            .GetProperty("id").GetGuid();

        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var response = await solicitante.GetAsync($"/api/users/{otherId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministradorPuedeConsultarDetalleDeCualquierUsuario()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var list = await ReadJsonAsync(await admin.GetAsync("/api/users"));
        var id = list.EnumerateArray()
            .First(u => u.GetProperty("correo").GetString() == ApiFactory.SolicitanteEmail)
            .GetProperty("id").GetGuid();

        var response = await admin.GetAsync($"/api/users/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(ApiFactory.SolicitanteEmail, json.GetProperty("correo").GetString());
    }

    [Fact]
    public async Task AdministradorPuedeActualizarNombreCorreoRolYEstado()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var userId = await CreateUserAsync(admin, "cambiar@test.local");

        var response = await admin.PutAsJsonAsync($"/api/users/{userId}", new
        {
            nombre = "Nombre Actualizado",
            correo = "actualizado@test.local",
            rol = "Aprobador",
            activo = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Nombre Actualizado", json.GetProperty("nombre").GetString());
        Assert.Equal("actualizado@test.local", json.GetProperty("correo").GetString());
        Assert.Equal("Aprobador", json.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task ActualizarUsuario_ConCorreoDeOtroUsuario_Devuelve409()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var userId = await CreateUserAsync(admin, "unico@test.local");

        var response = await admin.PutAsJsonAsync($"/api/users/{userId}", new
        {
            nombre = "Usuario",
            correo = ApiFactory.SolicitanteEmail,
            rol = "Solicitante",
            activo = true
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UsuarioPuedeActualizarSuNombreYCorreo()
    {
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);

        var response = await solicitante.PutAsJsonAsync("/api/users/me", new
        {
            nombre = "Solicitante Renombrado",
            correo = "renombrado@test.local"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Solicitante Renombrado", json.GetProperty("nombre").GetString());
        Assert.Equal("renombrado@test.local", json.GetProperty("correo").GetString());
        Assert.Equal("Solicitante", json.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task UsuarioNoPuedeCambiarSuPropioRolNiEstado()
    {
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);

        var rol = await solicitante.PutAsJsonAsync("/api/users/me", new
        {
            nombre = "Solicitante",
            correo = ApiFactory.SolicitanteEmail,
            rol = "Administrador"
        });
        Assert.Equal(HttpStatusCode.Forbidden, rol.StatusCode);

        var estado = await solicitante.PutAsJsonAsync("/api/users/me", new
        {
            nombre = "Solicitante",
            correo = ApiFactory.SolicitanteEmail,
            activo = false
        });
        Assert.Equal(HttpStatusCode.Forbidden, estado.StatusCode);
    }

    [Fact]
    public async Task DesactivarYReactivarUsuario_ControlaElInicioDeSesion()
    {
        var admin = await ClientForAsync(ApiFactory.AdminEmail);
        var userId = await CreateUserAsync(admin, "ciclo@test.local");

        var deactivate = await admin.PutAsJsonAsync($"/api/users/{userId}", new
        {
            nombre = "Usuario de Pruebas",
            correo = "ciclo@test.local",
            rol = "Solicitante",
            activo = false
        });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var failedLogin = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            correo = "ciclo@test.local",
            contrasena = ApiFactory.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);

        var reactivate = await admin.PutAsJsonAsync($"/api/users/{userId}", new
        {
            nombre = "Usuario de Pruebas",
            correo = "ciclo@test.local",
            rol = "Solicitante",
            activo = true
        });
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);

        var login = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            correo = "ciclo@test.local",
            contrasena = ApiFactory.Password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task RegistrarUsuario_ComoSolicitante_Devuelve403()
    {
        var solicitante = await ClientForAsync(ApiFactory.SolicitanteEmail);
        var response = await solicitante.PostAsJsonAsync("/api/users", new
        {
            nombre = "No Permitido",
            correo = "nopermitido@test.local",
            contrasena = "Password123!",
            rol = "Solicitante"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
