# AcademicSpace

Sistema web para gestionar la reserva de espacios académicos (laboratorios, auditorios y salas
de cómputo). Centraliza el registro de usuarios, la administración de espacios, la consulta de
disponibilidad, la prevención de reservas superpuestas, las aprobaciones, las cancelaciones y el
historial.

El proyecto es un monolito modular por capas:

- **Backend:** .NET 8 Web API, Entity Framework Core y MySQL (Pomelo).
- **Frontend:** React con TypeScript y Vite.
- **Pruebas:** xUnit con `WebApplicationFactory` sobre una base MySQL de pruebas.

## Requisitos

| Herramienta | Versión recomendada |
| --- | --- |
| .NET SDK | 8.0 o superior (el proyecto apunta a `net8.0`) |
| Node.js | 20 o superior |
| MySQL | 8.0 o superior |
| `dotnet-ef` | 8.0.13 (solo para crear o aplicar migraciones) |

## Estructura del proyecto

```text
AcademicSpace/
├── AcademicSpace.sln
├── src/AcademicSpace.Api/
│   ├── Modules/
│   │   ├── Users/          # Autenticación, usuarios, roles y sesiones
│   │   ├── Spaces/         # Espacios académicos
│   │   ├── Reservations/   # Solicitudes, conflictos, aprobaciones e historial
│   │   └── Availability/   # Consulta de disponibilidad
│   ├── Shared/
│   │   ├── Auth/           # JWT, roles y claims
│   │   ├── Errors/         # ProblemDetails y manejo global de errores
│   │   └── Persistence/    # DbContext, siembra y migraciones
│   └── Program.cs
├── tests/AcademicSpace.Tests/
│   ├── TestSupport/        # ApiFactory y utilidades de integración
│   └── *Tests.cs
├── frontend/
│   └── src/
│       ├── api/            # Cliente HTTP y tipos
│       ├── auth/           # Contexto de autenticación
│       ├── components/     # Layout y protección de rutas
│       └── pages/          # Pantallas por rol
├── docs/                   # API, modelo de datos, frontend y verificación
└── openspec/               # Especificaciones y tareas del cambio initial-system
```

## Configuración

La API lee su configuración de `appsettings.json`, `appsettings.{Environment}.json` y variables de
entorno. En producción todas las variables sensibles deben definirse por entorno.

| Variable | Obligatoria | Descripción |
| --- | --- | --- |
| `ConnectionStrings__Default` | Sí | Cadena de conexión MySQL de la aplicación. |
| `Jwt__Key` | Sí | Clave simétrica para firmar los JWT (mínimo 32 caracteres recomendado). |
| `Jwt__Issuer` | No | Emisor del token. Por defecto `AcademicSpace`. |
| `Jwt__Audience` | No | Audiencia del token. Por defecto `AcademicSpace`. |
| `Jwt__ExpirationHours` | No | Vigencia del token en horas. Por defecto `8`. |
| `Seed__AdminName` | No | Nombre del administrador inicial. |
| `Seed__AdminEmail` | No | Correo del administrador inicial. Por defecto `admin@academicspace.edu`. |
| `Seed__AdminPassword` | No | Contraseña del administrador inicial. Si se omite, se genera una y se registra en el log. |
| `Cors__AllowedOrigins__0` | No | Origen permitido del frontend. Por defecto `http://localhost:5173`. |
| `VITE_API_URL` | No | URL de la API usada por el frontend. Por defecto `http://localhost:5199`. |

`appsettings.Development.json` incluye valores locales de ejemplo (base MySQL en el puerto 3310,
clave JWT y contraseña del administrador). **No deben usarse en producción.**

### Base de datos local con Docker

```bash
docker run -d --name academicspace-mysql \
  -e MYSQL_ROOT_PASSWORD=academicspace \
  -e MYSQL_DATABASE=academicspace \
  -p 3310:3306 mysql:8.4

docker exec academicspace-mysql mysql -uroot -pacademicspace \
  -e "CREATE DATABASE IF NOT EXISTS academicspace_test;"
```

Si usa un MySQL instalado localmente, cree las bases `academicspace` y `academicspace_test` y ajuste
la cadena de conexión.

## Ejecución

### 1. Backend

Las migraciones se aplican automáticamente al iniciar la API (excepto en el entorno `Testing`).
Para aplicarlas manualmente:

```bash
dotnet tool install --global dotnet-ef --version 8.0.13
dotnet ef database update --project src/AcademicSpace.Api --startup-project src/AcademicSpace.Api
```

Con los valores de desarrollo incluidos:

```powershell
dotnet run --project src/AcademicSpace.Api --urls http://localhost:5199
```

Con variables de entorno explícitas:

```powershell
$env:ConnectionStrings__Default = "Server=127.0.0.1;Port=3310;Database=academicspace;User=root;Password=academicspace;AllowUserVariables=true"
$env:Jwt__Key = "una-clave-segura-de-al-menos-32-caracteres"
$env:Seed__AdminPassword = "Admin12345!"
dotnet run --project src/AcademicSpace.Api --urls http://localhost:5199
```

La API queda disponible en `http://localhost:5199`. El administrador inicial se crea en el primer
arranque si no existe.

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

La aplicación queda en `http://localhost:5173`. Para apuntar a otra API:

```bash
$env:VITE_API_URL = "http://localhost:5199"
npm run dev
```

### 3. Pruebas

Las pruebas de integración usan la base `academicspace_test` y aplican las migraciones antes de la
primera ejecución. La conexión se puede cambiar con `ACADEMICSPACE_TEST_DB`.

```bash
dotnet test
cd frontend && npm run build
```

## Flujo de autenticación

1. `POST /api/auth/login` recibe `correo` y `contrasena`.
2. El sistema verifica la contraseña con el hash almacenado y rechaza cuentas inactivas.
3. Se crea un registro en `Sesion` con un identificador único y su fecha de expiración.
4. Se emite un JWT firmado con `Jwt__Key` que incluye:
   - `sub` / `nameidentifier`: identificador del usuario.
   - `jti`: identificador de la sesión.
   - `role`: `Administrador`, `Solicitante` o `Aprobador`.
   - `iss`, `aud`, `exp`: emisor, audiencia y expiración.
5. En cada petición autenticada se validan firma, emisor, audiencia y expiración; además se
   comprueba que la sesión exista, no esté revocada y que el usuario siga activo.
6. `POST /api/auth/logout` revoca la sesión actual; las peticiones posteriores con ese token
   responden `401`.
7. Al desactivar un usuario se revocan todas sus sesiones de inmediato.

Ejemplo:

```bash
curl -X POST http://localhost:5199/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"correo":"admin@academicspace.edu","contrasena":"Admin12345!"}'

curl http://localhost:5199/api/users/me \
  -H "Authorization: Bearer <token>"
```

## Documentación adicional

- [docs/api.md](docs/api.md): endpoints, permisos, ejemplos y códigos de error.
- [docs/modelo-datos.md](docs/modelo-datos.md): entidades, relaciones, índices y conservación.
- [docs/frontend.md](docs/frontend.md): rutas, pantallas y roles.
- [docs/verificacion.md](docs/verificacion.md): resultados de pruebas y recorrido de extremo a extremo.

## Regla de duración de las reservas

Las reservas de espacios académicos tienen una duración máxima de cuatro horas consecutivas.

- Se permiten reservas de hasta cuatro horas, inclusive.
- Se rechazan solicitudes cuya duración supere las cuatro horas.
- La validación se realiza al crear una reserva.
- También se valida al aprobar una solicitud, para impedir la aprobación de reservas heredadas que superen el límite.
- Las consultas de disponibilidad rechazan franjas horarias superiores a cuatro horas.
- Las reservas heredadas que superen el límite deben rechazarse o cancelarse; no se modifican automáticamente.

Cuando se intenta crear una reserva o consultar disponibilidad con una duración superior al límite, la API responde `400 Bad Request`. Si se intenta aprobar una reserva heredada que supera el límite, responde `409 Conflict`.

## Alcance

Incluye autenticación, usuarios y roles, espacios, disponibilidad, solicitudes, conflictos,
aprobaciones, cancelaciones, historial y validación de la duración máxima de cuatro horas
consecutivas por reserva.

No incluye pagos, integraciones con sistemas académicos externos, aplicación móvil ni SMS.
