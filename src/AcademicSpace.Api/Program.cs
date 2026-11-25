using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AcademicSpace.Api.Modules.Availability.Services;
using AcademicSpace.Api.Modules.Reservations.Repositories;
using AcademicSpace.Api.Modules.Reservations.Services;
using AcademicSpace.Api.Modules.Spaces.Repositories;
using AcademicSpace.Api.Modules.Spaces.Services;
using AcademicSpace.Api.Modules.Users.Repositories;
using AcademicSpace.Api.Modules.Users.Services;
using AcademicSpace.Api.Shared.Auth;
using AcademicSpace.Api.Shared.Errors;
using AcademicSpace.Api.Shared.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "Frontend";

builder.Services.AddDbContext<AcademicSpaceDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Falta la cadena de conexión. Configure ConnectionStrings__Default.");
    }
    options.UseMySql(connectionString, MySqlServerVersionCache.Get(connectionString));
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISpaceRepository, SpaceRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SpaceService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<AvailabilityService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        if (string.IsNullOrWhiteSpace(jwt.Key))
        {
            throw new InvalidOperationException("Falta la clave JWT. Configure Jwt__Key.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sessionId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(sessionId) || !Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("La sesión no es válida.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AcademicSpaceDbContext>();
                var session = await db.Sesiones.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session is null || session.Revocada || session.FechaExpiracion <= DateTime.UtcNow)
                {
                    context.Fail("La sesión fue revocada o expiró.");
                    return;
                }

                var active = await db.Usuarios.AsNoTracking()
                    .AnyAsync(u => u.Id == userId && u.Activo);
                if (!active)
                {
                    context.Fail("La cuenta no está habilitada.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Valor inválido."
                            : error.ErrorMessage)
                        .ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Solicitud inválida",
                Detail = "Uno o más campos no son válidos.",
                Instance = context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(problem);
        };
    });

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.InitializeDatabaseAsync();
}

app.Run();

public partial class Program;
