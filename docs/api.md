# API de AcademicSpace

URL base local: `http://localhost:5199`. Todas las rutas usan JSON y, salvo el inicio de sesión,
requieren el encabezado:

```http
Authorization: Bearer <token>
```

## Roles y permisos

| Rol | Alcance |
| --- | --- |
| `Administrador` | Usuarios, espacios, consulta de reservas e historial completo. |
| `Solicitante` | Disponibilidad, creación y cancelación de sus propias reservas, su historial. |
| `Aprobador` | Disponibilidad, bandeja de pendientes, aprobación/rechazo, todas las reservas e historial. |

Cuando la petición no incluye un token válido la API responde `401`. Cuando el rol no corresponde
responde `403`. La validación de la sesión ocurre en cada petición autenticada.

## Formato de errores

Los errores usan `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Solicitud inválida",
  "status": 400,
  "detail": "Uno o más campos no son válidos.",
  "instance": "/api/reservations",
  "errors": {
    "Motivo": ["The Motivo field is required."]
  }
}
```

Los errores de negocio usan el mismo formato con `detail` descriptivo y el código correspondiente
(`400`, `401`, `403`, `404`, `409`).

---

## Autenticación

### `POST /api/auth/login`

Permiso: anónimo.

```json
{
  "correo": "admin@academicspace.edu",
  "contrasena": "Admin12345!"
}
```

Respuesta `200`:

```json
{
  "token": "<jwt>",
  "expiracion": "2026-11-25T07:00:00Z",
  "usuario": {
    "id": "…",
    "nombre": "Administrador",
    "correo": "admin@academicspace.edu",
    "rol": "Administrador",
    "activo": true,
    "fechaCreacion": "2026-11-24T22:51:00Z"
  }
}
```

Errores: `400` datos inválidos, `401` credenciales incorrectas o cuenta inactiva.

### `POST /api/auth/logout`

Permiso: autenticado. Revoca la sesión actual. Respuesta `204`. Las peticiones posteriores con el
mismo token responden `401`.

---

## Usuarios

### `GET /api/users`

Permiso: `Administrador`. Devuelve todos los usuarios sin contraseñas ni hashes.

```json
[
  {
    "id": "…",
    "nombre": "Ana Torres",
    "correo": "ana@academicspace.edu",
    "rol": "Solicitante",
    "activo": true,
    "fechaCreacion": "2026-11-24T22:51:00Z"
  }
]
```

### `POST /api/users`

Permiso: `Administrador`. No existe registro público.

```json
{
  "nombre": "Ana Torres",
  "correo": "ana@academicspace.edu",
  "contrasena": "Password123!",
  "rol": "Solicitante"
}
```

Respuesta `201` con el usuario creado. Errores: `400` nombre vacío, correo inválido, contraseña de
menos de 8 caracteres o rol inexistente; `409` correo duplicado.

### `GET /api/users/me`

Permiso: autenticado. Devuelve el perfil propio.

### `PUT /api/users/me`

Permiso: autenticado. Solo permite nombre y correo.

```json
{ "nombre": "Ana Torres", "correo": "ana@academicspace.edu" }
```

Errores: `400` datos inválidos, `403` si se intenta enviar `rol` o `activo`, `409` correo en uso.

### `GET /api/users/{id}`

Permiso: `Administrador`. `404` si no existe.

### `PUT /api/users/{id}`

Permiso: `Administrador`. Actualiza nombre, correo, rol y estado.

```json
{
  "nombre": "Ana Torres",
  "correo": "ana@academicspace.edu",
  "rol": "Aprobador",
  "activo": true
}
```

Errores: `400` datos inválidos, `404` usuario inexistente, `409` correo duplicado. Al pasar de
activo a inactivo se revocan todas las sesiones del usuario.

---

## Espacios académicos

Tipos aceptados (JSON numérico): `1` Laboratorio, `2` Auditorio, `3` Sala de cómputo. En las
respuestas se devuelve el nombre (`Laboratorio`, `Auditorio`, `SalaComputo`).

### `GET /api/spaces`

Permiso: autenticado. Un administrador recibe todos los espacios (activos e inactivos); los demás
roles solo reciben espacios activos. Filtro opcional `?tipo=1`.

### `GET /api/spaces/{id}`

Permiso: autenticado. Un espacio inactivo responde `404` para roles distintos de administrador.

### `POST /api/spaces`

Permiso: `Administrador`.

```json
{
  "nombre": "Laboratorio de Redes",
  "tipo": 1,
  "ubicacion": "Bloque B, piso 2",
  "capacidad": 30
}
```

Respuesta `201`:

```json
{
  "id": "…",
  "nombre": "Laboratorio de Redes",
  "tipo": "Laboratorio",
  "ubicacion": "Bloque B, piso 2",
  "capacidad": 30,
  "activo": true
}
```

Errores: `400` tipo fuera de 1–3 o capacidad menor o igual a cero; `409` nombre duplicado.

### `PUT /api/spaces/{id}`

Permiso: `Administrador`. Actualiza nombre, tipo, ubicación, capacidad y estado.

```json
{
  "nombre": "Laboratorio de Redes",
  "tipo": 1,
  "ubicacion": "Bloque B, piso 2",
  "capacidad": 35,
  "activo": false
}
```

Errores: `400`, `404`, `409` nombre duplicado.

### Eliminación

No existe `DELETE /api/spaces/{id}`; responde `405`. La baja se realiza con `activo: false`. Un
espacio inactivo deja de aparecer en disponibilidad y no acepta nuevas solicitudes, pero sus
reservas existentes se conservan.

---

## Disponibilidad

Semántica: una franja está ocupada si se superpone con una reserva en estado `Pendiente` o
`Aprobada` del mismo espacio y fecha. Las reservas `Rechazada` y `Cancelada` no bloquean. La
comparación usa intervalos semiabiertos:

```text
horaInicio < horaFinAjena  AND  horaFin > horaInicioAjena
```

Por eso 08:00–10:00 y 10:00–12:00 no se consideran superpuestas. Los espacios inactivos nunca se
ofrecen como disponibles. La respuesta de disponibilidad no expone datos del solicitante.

### `GET /api/availability/spaces`

Parámetros obligatorios: `fecha` (`YYYY-MM-DD`), `horaInicio` y `horaFin` (`HH:mm` o `HH:mm:ss`).
Parámetro opcional: `tipo` (1–3).

```http
GET /api/availability/spaces?fecha=2026-12-01&horaInicio=08:00&horaFin=10:00&tipo=1
```

Respuesta `200`:

```json
{
  "espacios": [
    {
      "id": "…",
      "nombre": "Laboratorio de Redes",
      "tipo": "Laboratorio",
      "ubicacion": "Bloque B, piso 2",
      "capacidad": 30,
      "activo": true
    }
  ],
  "mensaje": "Espacios disponibles."
}
```

Si no hay espacios: `espacios: []` y `mensaje: "No hay espacios disponibles en la franja consultada."`.

Errores:
- `400` si la fecha es pasada.
- `400` si la hora de fin no es posterior a la hora de inicio.
- `400` si la franja consultada supera las cuatro horas consecutivas.
- `400` si el tipo de espacio no es válido.

### `GET /api/availability/spaces/{id}`

Mismos parámetros de fecha y horas, sin `tipo`.

```json
{
  "espacioId": "…",
  "espacioNombre": "Laboratorio de Redes",
  "disponible": false,
  "ocupaciones": [
    { "horaInicio": "08:00:00", "horaFin": "10:00:00" }
  ]
}
```

Un espacio inactivo devuelve `disponible: false`.

Errores:
- `400` si los parámetros son inválidos o la franja supera las cuatro horas consecutivas.
- `404` si el espacio no existe.

---

## Solicitudes de reserva

Estados: `Pendiente`, `Aprobada`, `Rechazada`, `Cancelada`.

```text
Pendiente ──aprobar──> Aprobada ──cancelar──> Cancelada
    ├──rechazar──> Rechazada
    └──cancelar──> Cancelada
```

Las transiciones no listadas se rechazan con `409`.

### `POST /api/reservations`

Permiso: `Solicitante`.

```json
{
  "espacioId": "…",
  "fecha": "2026-12-01",
  "horaInicio": "08:00:00",
  "horaFin": "10:00:00",
  "motivo": "Clase de algoritmos"
}
```

Respuesta `201`:

```json
{
  "id": "…",
  "espacioId": "…",
  "espacioNombre": "Laboratorio de Redes",
  "espacioTipo": "Laboratorio",
  "solicitanteId": "…",
  "solicitanteNombre": "Ana Torres",
  "fecha": "2026-12-01",
  "horaInicio": "08:00:00",
  "horaFin": "10:00:00",
  "motivo": "Clase de algoritmos",
  "estado": "Pendiente",
  "fechaSolicitud": "2026-11-24T23:00:00Z",
  "aprobadorId": null,
  "aprobadorNombre": null,
  "fechaDecision": null,
  "motivoRechazo": null,
  "fechaCancelacion": null
}
```

Errores:
- `400` si el espacio está vacío, la fecha es pasada, la hora final no es posterior a la inicial, el motivo está vacío o la duración supera las cuatro horas consecutivas.
- `403` si el rol es distinto de solicitante.
- `404` si el espacio no existe.
- `409` si el espacio está inactivo o la franja está ocupada.

Ejemplo de conflicto:

```json
{
  "title": "Conflicto",
  "status": 409,
  "detail": "El espacio ya está reservado en esa franja horaria.",
  "instance": "/api/reservations"
}
```

La creación es transaccional: bloquea la fila del espacio, comprueba conflictos e inserta. Dos
solicitudes simultáneas superpuestas producen una sola reserva; la otra recibe `409`.

### `GET /api/reservations`

Permiso: autenticado. El solicitante solo ve sus solicitudes; aprobador y administrador ven todas.
Orden: `fechaSolicitud` descendente.

### `GET /api/reservations/pending`

Permiso: `Aprobador`. Solicitudes en estado `Pendiente`, de la más antigua a la más reciente.

### `GET /api/reservations/history`

Permiso: autenticado. Historial con el mismo alcance que `GET /api/reservations`: el solicitante ve
solo las propias y los demás roles ven todas. Incluye reservas aprobadas, rechazadas y canceladas;
ningún registro se elimina.

### `GET /api/reservations/{id}`

Permiso: autenticado. Un solicitante solo puede consultar sus propias reservas (`403` en caso
contrario). `404` si no existe.

---

## Aprobaciones y rechazos

### `POST /api/reservations/{id}/approve`

Permiso: `Aprobador`. Sin cuerpo.

- Respuesta `200` con `estado: "Aprobada"`, `aprobadorId`, `aprobadorNombre` y `fechaDecision`.
- `403` para otros roles.
- `404` si no existe.
- `409` si la solicitud ya no está pendiente, si al revalidar se superpone con otra reserva o si su duración supera las cuatro horas consecutivas.

La aprobación abre una transacción, bloquea el espacio y revalida la duración máxima y la superposición antes de cambiar el estado. Si una reserva heredada supera las cuatro horas, permanece en estado `Pendiente` y no puede aprobarse.

### `POST /api/reservations/{id}/reject`

Permiso: `Aprobador`.

```json
{ "motivo": "Documentación incompleta" }
```

- Respuesta `200` con `estado: "Rechazada"`, `motivoRechazo`, `aprobadorId` y `fechaDecision`.
- `400` si el motivo está vacío.
- `403` para otros roles.
- `409` si la solicitud ya fue decidida.

---

## Cancelaciones e historial

### `POST /api/reservations/{id}/cancel`

Permiso: `Solicitante` propietario de la reserva. Sin cuerpo.

- Respuesta `200` con `estado: "Cancelada"` y `fechaCancelacion`.
- `403` si la reserva pertenece a otro usuario.
- `404` si no existe.
- `409` si la reserva ya está `Rechazada` o `Cancelada`.

Al cancelar una reserva `Pendiente` o `Aprobada`, la franja vuelve a estar disponible y puede
recibir una nueva solicitud.

Las reservas rechazadas y canceladas permanecen consultables en `GET /api/reservations/history` y
en el detalle; no se eliminan del sistema.
