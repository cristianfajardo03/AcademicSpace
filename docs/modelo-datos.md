# Modelo de datos

AcademicSpace usa MySQL 8 con Entity Framework Core (proveedor Pomelo). La migración inicial
`20261124225440_InitialCreate` crea las tablas, relaciones e índices descritos a continuación.

## Diagrama de relaciones

```text
Rol 1 ──── * Usuario 1 ──── * Sesion
                 │
                 ├──── * Reserva (Solicitante)
                 └──── * Reserva (Aprobador, opcional)

Espacio 1 ──── * Reserva
```

## Tablas

### `Rol`

Catálogo cerrado de roles del sistema. Se siembra en la migración.

| Columna | Tipo | Restricciones |
| --- | --- | --- |
| `Id` | `int` | PK, autoincremental |
| `Nombre` | `varchar(40)` | Obligatorio, único |

Valores sembrados: `Administrador` (1), `Solicitante` (2), `Aprobador` (3).

### `Usuario`

Cuenta de acceso. Un usuario tiene exactamente un rol.

| Columna | Tipo | Restricciones |
| --- | --- | --- |
| `Id` | `char(36)` | PK (GUID) |
| `Nombre` | `varchar(120)` | Obligatorio |
| `Correo` | `varchar(200)` | Obligatorio, único |
| `HashContrasena` | `varchar(500)` | Obligatorio; hash de ASP.NET Identity |
| `RolId` | `int` | FK a `Rol`, `RESTRICT` |
| `Activo` | `tinyint(1)` | Obligatorio; por defecto `true` |
| `FechaCreacion` | `datetime(6)` | Obligatorio |
| `FechaActualizacion` | `datetime(6)` | Obligatorio |

Índices: `IX_Usuario_Correo` (único), `IX_Usuario_RolId`.

### `Sesion`

Sesión emitida al iniciar sesión. Permite revocar tokens antes de su expiración.

| Columna | Tipo | Restricciones |
| --- | --- | --- |
| `Id` | `varchar(64)` | PK; identificador incluido en el claim `jti` |
| `UsuarioId` | `char(36)` | FK a `Usuario`, `CASCADE` |
| `FechaInicio` | `datetime(6)` | Obligatorio |
| `FechaExpiracion` | `datetime(6)` | Obligatorio |
| `Revocada` | `tinyint(1)` | Obligatorio; por defecto `false` |

Índice: `IX_Sesion_UsuarioId`.

### `Espacio`

Espacio académico reservable.

| Columna | Tipo | Restricciones |
| --- | --- | --- |
| `Id` | `char(36)` | PK (GUID) |
| `Nombre` | `varchar(150)` | Obligatorio, único |
| `Tipo` | `varchar(30)` | Obligatorio; `Laboratorio`, `Auditorio` o `SalaComputo` |
| `Ubicacion` | `varchar(200)` | Obligatorio |
| `Capacidad` | `int` | Obligatorio, mayor que cero (validado en el servicio) |
| `Activo` | `tinyint(1)` | Obligatorio; por defecto `true` |
| `FechaCreacion` | `datetime(6)` | Obligatorio |
| `FechaActualizacion` | `datetime(6)` | Obligatorio |

Índice: `IX_Espacio_Nombre` (único).

El tipo se almacena como texto para que la base refleje los valores del dominio y no dependa de
posiciones numéricas.

### `Reserva`

Solicitud de reserva y su trazabilidad.

| Columna | Tipo | Restricciones |
| --- | --- | --- |
| `Id` | `char(36)` | PK (GUID) |
| `EspacioId` | `char(36)` | FK a `Espacio`, `RESTRICT` |
| `SolicitanteId` | `char(36)` | FK a `Usuario`, `RESTRICT` |
| `Fecha` | `date` | Obligatorio |
| `HoraInicio` | `time(6)` | Obligatorio |
| `HoraFin` | `time(6)` | Obligatorio |
| `Motivo` | `varchar(500)` | Obligatorio |
| `Estado` | `varchar(20)` | Obligatorio; `Pendiente`, `Aprobada`, `Rechazada` o `Cancelada` |
| `FechaSolicitud` | `datetime(6)` | Obligatorio |
| `AprobadorId` | `char(36)` | FK a `Usuario`, `RESTRICT`, opcional |
| `FechaDecision` | `datetime(6)` | Opcional |
| `MotivoRechazo` | `varchar(500)` | Opcional |
| `FechaCancelacion` | `datetime(6)` | Opcional |
| `Version` | `int` | Token de concurrencia optimista |

Índices: `IX_Reserva_EspacioId_Fecha_Estado` (consultas de disponibilidad y superposición),
`IX_Reserva_SolicitanteId_FechaSolicitud` (historial del solicitante) y `IX_Reserva_AprobadorId`.

`Fecha` y las horas se guardan como valores locales (`DATE` y `TIME`), sin conversión de zona
horaria. La aplicación compara y muestra los mismos valores que recibe.

## Estados y transiciones

```text
Pendiente ──aprobar──> Aprobada ──cancelar──> Cancelada
    │
    ├──rechazar──> Rechazada
    └──cancelar──> Cancelada
```

- `Aprobada`, `Rechazada` y `Cancelada` son estados finales.
- Una reserva `Rechazada` o `Cancelada` no bloquea la franja.
- Una reserva `Pendiente` o `Aprobada` sí participa en la detección de superposiciones.

La regla de superposición usa intervalos semiabiertos sobre el mismo espacio y fecha:

```text
horaInicio < horaFinAjena  AND  horaFin > horaInicioAjena
```

Esto permite reservas adyacentes (por ejemplo, 08:00–10:00 y 10:00–12:00) y evita solapamientos parciales o totales.

### Duración máxima de las reservas

Toda reserva debe tener una duración máxima de cuatro horas consecutivas.

- Las reservas de hasta cuatro horas, inclusive, son válidas.
- Las reservas superiores a cuatro horas se rechazan.
- La validación se realiza al crear una solicitud y al aprobarla.
- Las consultas de disponibilidad también rechazan franjas superiores a cuatro horas.
- Las reservas heredadas que superen el límite no pueden aprobarse y deben rechazarse o cancelarse.

Esta regla pertenece a la lógica de negocio y se implementa en `ReservationRules`, `ReservationService` y `AvailabilityService`. No requiere columnas adicionales, restricciones SQL ni migraciones.

## Concurrencia

- La creación de una solicitud abre una transacción `READ COMMITTED`, bloquea la fila del espacio con `SELECT ... FOR UPDATE`, comprueba superposiciones e inserta la reserva.
- La aprobación repite el bloqueo del espacio y la comprobación antes de cambiar el estado, para cubrir reservas que hayan entrado entre la solicitud y la decisión.
- Durante la aprobación también se verifica que la duración de la reserva no supere las cuatro horas consecutivas.
- `Reserva.Version` es un token de concurrencia optimista que protege contra actualizaciones simultáneas de la misma reserva.

La duración máxima se valida en la aplicación y no modifica el mecanismo de concurrencia ni el esquema de la base de datos.

## Reglas de conservación

- **No hay borrado físico** de usuarios, espacios ni reservas. No existen endpoints `DELETE`.
- Un usuario se desactiva con `Activo = false`; al hacerlo se revocan todas sus sesiones.
- Un espacio se desactiva con `Activo = false`; deja de ofrecerse para nuevas solicitudes y
  disponibilidad, pero sus reservas existentes conservan su estado y siguen siendo consultables.
- Una reserva se cancela o rechaza cambiando su estado; nunca se elimina, por lo que permanece en
  el historial.
- Las claves foráneas usan `RESTRICT` (salvo `Sesion`, que usa `CASCADE`) para impedir borrados en
  cascada accidentales.
