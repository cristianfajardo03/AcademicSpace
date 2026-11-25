# Design

## Context

AcademicSpace parte de un repositorio sin código de aplicación: solo existen `PLAN_INGENIERIA.md` y la configuración de OpenSpec. El diseño debe respetar las decisiones ya tomadas en el plan: monolito modular por capas, .NET 8 Web API, React con TypeScript, MySQL, Entity Framework Core y xUnit. Ver `proposal.md` para la motivación y `specs/` para el comportamiento exigido.

Restricciones que condicionan el diseño:

- La regla de máximo cuatro horas consecutivas no forma parte de este cambio.
- No se implementan pagos, integraciones externas, aplicación móvil ni notificaciones SMS.
- Los roles son fijos: administrador, usuario solicitante y responsable de aprobación.
- La prevención de superposiciones debe funcionar ante solicitudes concurrentes.
- El historial debe conservarse; las reservas no se eliminan.

## Goals / Non-Goals

**Goals:**

- Definir una estructura de solución y módulos que permita implementar las ocho capacidades sin acoplamiento excesivo.
- Establecer el modelo de datos, las restricciones y la estrategia transaccional que garantizan la ausencia de reservas superpuestas.
- Definir el contrato REST, la autenticación, la autorización por rol y el manejo uniforme de errores.
- Definir la organización del frontend y la estrategia de pruebas.

**Non-Goals:**

- Incorporar la regla de duración máxima de cuatro horas.
- Añadir recuperación de contraseña, registro público, notificaciones, pagos, reportes o funcionalidades no especificadas.
- Definir wireframes, diseño visual detallado o textos finales de la interfaz.
- Definir despliegue en producción, CI/CD o infraestructura en la nube.

## Decisions

### 1. Estructura del backend: monolito modular por capas

Se usará una solución .NET 8 con dos proyectos:

- `AcademicSpace.Api`: proyecto Web API que contiene los módulos y las capas.
- `AcademicSpace.Tests`: proyecto xUnit con pruebas unitarias y de integración.

Dentro de `AcademicSpace.Api` se organizará por módulo y, dentro de cada módulo, por capa:

```text
Modules/
  Users/
    Controllers/
    Services/
    Contracts/
    Entities/          (solo si el módulo posee entidades propias)
    Repositories/
  Spaces/
    Controllers/
    Services/
    Contracts/
    Repositories/
  Reservations/
    Controllers/
    Services/
    Contracts/
    Repositories/
  Availability/
    Controllers/
    Services/
    Contracts/
Shared/
  Auth/
  Persistence/
  Errors/
  Validation/
```

- **Controllers** exponen los endpoints REST y no contienen reglas de negocio.
- **Services** implementan reglas, validaciones, transiciones de estado y autorización de negocio.
- **Repositories** encapsulan el acceso a EF Core y las consultas de superposición/disponibilidad.
- **Contracts** contienen DTO de entrada y salida; las entidades nunca se exponen directamente.

Se descarta separar Domain/Application/Infrastructure en proyectos independientes porque el alcance académico no lo requiere y el plan pide evitar complejidad innecesaria. Se descartan microservicios por la misma razón.

### 2. Modelo de datos y persistencia

Entidades principales (nombres de tabla en singular):

**Rol**: `Id`, `Nombre` (único). Se siembran exactamente `Administrador`, `Solicitante` y `Aprobador`.

**Usuario**: `Id`, `Nombre`, `Correo` (único), `HashContrasena`, `RolId`, `Activo`, `FechaCreacion`, `FechaActualizacion`.

**Espacio**: `Id`, `Nombre` (único), `Tipo` (`Laboratorio`, `Auditorio`, `SalaComputo`), `Ubicacion`, `Capacidad`, `Activo`, `FechaCreacion`, `FechaActualizacion`.

**Reserva**: `Id`, `EspacioId`, `SolicitanteId`, `Fecha` (`DATE`), `HoraInicio` (`TIME`), `HoraFin` (`TIME`), `Motivo`, `Estado` (`Pendiente`, `Aprobada`, `Rechazada`, `Cancelada`), `FechaSolicitud`, `AprobadorId` (nullable), `FechaDecision` (nullable), `MotivoRechazo` (nullable), `FechaCancelacion` (nullable), `Version` (token de concurrencia optimista).

**Sesion**: `Id` (identificador del token, `jti`), `UsuarioId`, `FechaInicio`, `FechaExpiracion`, `Revocada`. Permite invalidar la sesión al cerrar sesión o al desactivar un usuario.

Decisiones de persistencia:

- Se usa Pomelo.EntityFrameworkCore.MySql como proveedor de EF Core para MySQL.
- Las fechas y horas de reserva se almacenan como `DATE` y `TIME` sin conversión de zona horaria; la institución opera en una única zona local. Esto evita corrimientos por UTC o horario de verano.
- El estado de la reserva se almacena como texto para facilitar consultas y legibilidad.
- Índices: `Usuario.Correo` único, `Espacio.Nombre` único, `Reserva (EspacioId, Fecha, Estado)` para detectar superposiciones y `Reserva (SolicitanteId, FechaSolicitud)` para historial.
- No se eliminan usuarios, espacios ni reservas: se usan `Activo` y los estados finales.
- Las migraciones de EF Core crean el esquema y siembran los tres roles.

### 3. Prevención de superposiciones concurrentes

MySQL no ofrece una restricción de exclusión por rangos, por lo que la garantía se implementa en la capa de servicio dentro de una transacción:

1. Abrir transacción con nivel `READ COMMITTED`.
2. Bloquear la fila del espacio con `SELECT ... FOR UPDATE` (solo si está activo).
3. Consultar si existe una reserva `Pendiente` o `Aprobada` del mismo espacio y fecha que cumpla `HoraInicio < finSolicitado AND HoraFin > inicioSolicitado`.
4. Si existe, revertir y responder `409 Conflict` sin insertar.
5. Si no existe, insertar o actualizar y confirmar.

El bloqueo de la fila del espacio serializa las operaciones de reserva de ese espacio, incluidas la creación y la aprobación. Las reservas de espacios distintos no se bloquean entre sí. Las solicitudes simultáneas se resuelven con una sola ganadora.

La aprobación reutiliza el mismo bloqueo y la misma comprobación antes de cambiar el estado. La cancelación actualiza el estado con control de concurrencia (`Version`) y no requiere bloqueo del espacio, porque liberar una franja no puede crear un conflicto.

Se descarta una tabla de bloques horarios pregenerados: añade complejidad y no cubre franjas arbitrarias. Se descarta confiar solo en una consulta previa sin bloqueo: dos transacciones podrían pasar la validación a la vez.

### 4. Autenticación y autorización

- Login con correo y contraseña; contraseña verificada contra un hash PBKDF2 generado con `PasswordHasher` de ASP.NET Core.
- Emisión de JWT firmado (HMAC SHA-256) con `sub` (usuario), `jti` (sesión) y `role`. Expiración configurable, por defecto 8 horas.
- Cada solicitud autenticada valida la firma, la expiración y que la sesión exista, no esté revocada y el usuario siga activo.
- Logout marca la sesión como revocada. La desactivación de un usuario revoca todas sus sesiones.
- Autorización con políticas por rol en los endpoints: `Administrador`, `Solicitante`, `Aprobador`. El rol se valida en el backend; el frontend solo oculta acciones, nunca es la única barrera.
- No existe registro público: el administrador crea las cuentas. Para el arranque se siembra un administrador inicial a partir de variables de entorno, sin credenciales hardcodeadas.
- Mensajes de login genéricos para no revelar si el correo existe.

Se descarta autenticación por cookies de servidor porque el frontend será una SPA separada y el consumo de API con JWT es directo. Se descarta JWT puramente stateless sin registro de sesión porque el requisito exige rechazar una sesión después del logout.

### 5. API REST

Prefijo `/api`. Respuestas de error con `ProblemDetails` (RFC 7807) y código HTTP coherente: `400` validación, `401` no autenticado, `403` permisos, `404` no encontrado, `409` conflicto de horario, duplicado o transición inválida.

| Método | Ruta | Rol | Propósito |
|---|---|---|---|
| POST | `/api/auth/login` | Público | Iniciar sesión |
| POST | `/api/auth/logout` | Autenticado | Cerrar sesión |
| GET | `/api/users` | Administrador | Listar usuarios |
| POST | `/api/users` | Administrador | Registrar usuario |
| GET | `/api/users/{id}` | Administrador | Detalle de usuario |
| PUT | `/api/users/{id}` | Administrador | Actualizar usuario, rol o estado |
| GET | `/api/users/me` | Autenticado | Consultar perfil propio |
| PUT | `/api/users/me` | Autenticado | Actualizar nombre o correo propios |
| GET | `/api/spaces` | Autenticado | Listar espacios activos (administrador: todos) |
| POST | `/api/spaces` | Administrador | Registrar espacio |
| GET | `/api/spaces/{id}` | Autenticado | Detalle de espacio |
| PUT | `/api/spaces/{id}` | Administrador | Modificar espacio o estado |
| GET | `/api/availability/spaces` | Autenticado | Espacios disponibles por fecha, franja y tipo |
| GET | `/api/availability/spaces/{id}` | Autenticado | Disponibilidad y franjas ocupadas de un espacio |
| POST | `/api/reservations` | Solicitante | Crear solicitud |
| GET | `/api/reservations` | Autenticado | Listar según rol (propias o todas) |
| GET | `/api/reservations/{id}` | Autenticado | Detalle según permisos |
| POST | `/api/reservations/{id}/cancel` | Solicitante | Cancelar reserva propia |
| GET | `/api/reservations/pending` | Aprobador | Solicitudes pendientes |
| POST | `/api/reservations/{id}/approve` | Aprobador | Aprobar solicitud |
| POST | `/api/reservations/{id}/reject` | Aprobador | Rechazar solicitud con motivo |
| GET | `/api/reservations/history` | Autenticado | Historial según rol |

La consulta de disponibilidad devuelve el estado del espacio y las franjas ocupadas, nunca datos del solicitante. No se define paginación porque el alcance no la solicita; las listas se ordenan por fecha de solicitud descendente, salvo pendientes, que se ordenan de más antigua a más reciente.

### 6. Frontend

Aplicación SPA con React, TypeScript y Vite, organizada por funcionalidades:

```text
src/
  app/            rutas, layout, providers
  features/
    auth/
    users/
    spaces/
    availability/
    reservations/
    approvals/
    history/
  shared/         cliente HTTP, modelos, componentes y utilidades
```

- El cliente HTTP centraliza la URL base, el token y el manejo de `401`/`403`.
- El token se mantiene en `sessionStorage` y se elimina al cerrar sesión.
- Las rutas se protegen por rol en el cliente, sin sustituir la autorización del backend.
- Tras una mutación se refrescan las consultas afectadas para reflejar estados y disponibilidad.
- No se incorpora una librería de estado global: el alcance se cubre con estado local y un contexto de autenticación.

### 7. Pruebas con xUnit

- **Unitarias**: cálculo de superposición (incluidos intervalos adyacentes), transiciones de estado, reglas de cancelación, autorización por rol y validaciones de entrada.
- **Integración**: endpoints contra una base MySQL de pruebas, aplicando migraciones; cubren login/logout, CRUD de usuarios y espacios, creación y consulta de reservas, aprobación, rechazo, cancelación e historial.
- **Concurrencia**: dos solicitudes superpuestas enviadas en paralelo; se verifica que solo una se registra.
- **Regresión de alcance**: se verifica que no exista una restricción de cuatro horas en este cambio.

Las pruebas de integración usan `WebApplicationFactory` y una base de datos de prueba configurada por variables de entorno. No se añaden contenedores ni servicios externos.

## Risks / Trade-offs

- **Superposiciones por escrituras concurrentes** → transacción con bloqueo de la fila del espacio y prueba de concurrencia obligatoria.
- **Escrituras directas a MySQL que omitan las reglas** → todo acceso pasa por la API; el usuario de base de datos de la aplicación no se usa para cargas manuales.
- **Costo de validar la sesión en cada solicitud** → una consulta indexada por `jti`; aceptable para el volumen institucional y necesaria para soportar logout y desactivación inmediata.
- **JWT y revocación** → la tabla `Sesion` añade persistencia, pero evita sesiones válidas después del logout.
- **Zona horaria y horario de verano** → se almacenan fecha y hora locales sin conversión; se documenta que el sistema opera en una sola zona.
- **Migración de esquema en una base existente** → el proyecto es nuevo; el despliegue inicial crea el esquema desde cero y el rollback consiste en eliminar la base de datos de desarrollo.
- **Duplicación de reglas de autorización entre frontend y backend** → el backend es la única fuente de verdad; las pruebas de integración cubren los rechazos `403`.
- **Crecimiento del historial sin paginación** → el alcance actual no lo exige; se podrá incorporar en un cambio posterior si el volumen lo requiere.

## Migration Plan

1. Crear la solución y los proyectos.
2. Configurar la cadena de conexión de MySQL y las migraciones de EF Core.
3. Aplicar la migración inicial y sembrar los roles y el administrador inicial.
4. Implementar los módulos en el orden de `tasks.md`.
5. Ejecutar pruebas unitarias y de integración.
6. Desplegar el backend y el frontend en el entorno de desarrollo institucional.

Rollback: al ser un sistema nuevo, se puede eliminar la base de datos y revertir los artefactos de despliegue; no existen datos productivos que migrar.

## Open Questions

Ninguna pendiente que afecte el alcance, las especificaciones o la implementación. Las decisiones menores no especificadas (por ejemplo, longitud exacta del token o nombre del entorno de pruebas) se resolverán durante la implementación sin alterar el comportamiento acordado.
