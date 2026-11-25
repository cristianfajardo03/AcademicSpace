# Verificación

Fecha de la verificación: 24 de noviembre de 2026.

Entorno utilizado:

- Windows 11, PowerShell 7.
- .NET SDK 9.0.203 compilando el proyecto `net8.0` con el runtime 8.0.15.
- Node.js 24.21.0 y npm 11.19.0.
- MySQL 8.4 en Docker, puerto 3310, bases `academicspace` y `academicspace_test`.

## 1. Compilación y pruebas automatizadas

Comandos utilizados:

```bash
dotnet test
cd frontend && npm run build
```

Resultados de la verificación inicial:

- `dotnet test`: **96 pruebas superadas, 0 con error, 0 omitidas**.
- `npm run build`: compilación de TypeScript y Vite finalizada sin errores.

Posteriormente, para el cambio `reservation-max-duration`, se ejecutaron las pruebas específicas de duración:

```powershell
dotnet test --filter "FullyQualifiedName~DurationRuleTests"
```

Resultado: **9 pruebas superadas, 0 con error**.

Las pruebas cubren autenticación, revocación de sesiones, usuarios, espacios, disponibilidad, solicitudes, conflictos, concurrencia, aprobaciones, rechazos, cancelaciones, historial, siembra y validación de duración máxima.

### Verificación en una copia limpia

Se copió el proyecto a un directorio temporal excluyendo `.git`, `.vs`, `bin`, `obj`, `node_modules` y `dist`, y se ejecutaron los comandos documentados:

```bash
dotnet build
cd frontend && npm ci && npm run build
```

Resultado: `dotnet build` terminó con 0 errores y `npm run build` generó `dist/` correctamente.

## 2. Recorrido de extremo a extremo

Se ejecutó un recorrido automatizado de interfaz con Playwright y Microsoft Edge en modo headless, contra la API en `http://localhost:5199` y la compilación de producción servida en `http://localhost:5173`.

| Paso | Resultado |
| --- | --- |
| Inicio de sesión con el administrador sembrado | Correcto |
| Registro de un usuario solicitante y uno aprobador | Correcto |
| Edición de un usuario por el administrador | Correcto |
| Registro de un espacio (laboratorio) | Correcto |
| Edición, desactivación y reactivación del espacio | Correcto |
| Cierre de sesión del administrador | Correcto |
| El solicitante no ve las opciones ni las rutas de administrador | Correcto |
| Consulta de disponibilidad por fecha, franja y tipo | Correcto |
| Detalle de franjas ocupadas del espacio | Correcto (sin ocupaciones antes de reservar) |
| Creación de una solicitud de reserva | Correcto (estado pendiente) |
| Segundo intento en la misma franja | Correcto (error de conflicto mostrado) |
| Aprobación de la solicitud por el aprobador | Correcto |
| Consulta de todas las reservas por el aprobador | Correcto |
| Cancelación de la reserva por el solicitante | Correcto |
| La reserva cancelada aparece en el historial | Correcto |
| La franja cancelada vuelve a estar disponible | Correcto |
| Sesión expirada redirige al inicio de sesión | Correcto |

Los únicos errores de consola registrados corresponden a las respuestas esperadas `409` (conflicto provocado) y `401` (sesión invalidada para probar la redirección).

Para reproducir el recorrido manualmente:

1. Iniciar la API y el frontend según el `README.md`.
2. Entrar con el administrador (`Seed__AdminEmail` / `Seed__AdminPassword`).
3. Crear un solicitante, un aprobador y un espacio.
4. Entrar como solicitante, consultar disponibilidad y crear una reserva.
5. Intentar una segunda reserva en la misma franja y comprobar el error de conflicto.
6. Entrar como aprobador y aprobar la solicitud.
7. Volver como solicitante, cancelar la reserva y revisar el historial y la disponibilidad.

## 3. Validación de la duración máxima de cuatro horas

Se implementó una regla de negocio que establece una duración máxima de cuatro horas consecutivas para las reservas académicas.

La validación se realiza en tres puntos:

- Creación de reservas, mediante `ReservationService`.
- Aprobación de reservas, mediante `ReservationService`.
- Consulta de disponibilidad, mediante `AvailabilityService`.

La regla se centraliza en `ReservationRules`, utilizando la constante `MaxDuracion` y el método `ExceedsMaxDuration`.

### Comportamiento esperado

- Las reservas de menos de cuatro horas están permitidas.
- Las reservas de exactamente cuatro horas están permitidas.
- Las reservas que superan las cuatro horas son rechazadas.
- Las reservas heredadas que superan el límite no pueden aprobarse.
- Las consultas de disponibilidad superiores a cuatro horas son rechazadas.

### Pruebas ejecutadas

| Prueba | Resultado esperado | Resultado obtenido |
| --- | --- | --- |
| Reserva de tres horas | `201 Created` | Correcto |
| Reserva de exactamente cuatro horas | `201 Created` | Correcto |
| Reserva de cuatro horas y un minuto | `400 Bad Request` | Correcto |
| Reserva de seis horas | `400 Bad Request` | Correcto |
| Aprobación de reserva heredada de seis horas | `409 Conflict` | Correcto |
| La reserva heredada conserva estado pendiente | `Pendiente` | Correcto |
| Consulta de espacios disponibles durante cuatro horas | `200 OK` | Correcto |
| Consulta de espacios disponibles durante más de cuatro horas | `400 Bad Request` | Correcto |
| Consulta de disponibilidad de un espacio durante cuatro horas | `200 OK` | Correcto |
| Consulta de disponibilidad de un espacio durante más de cuatro horas | `400 Bad Request` | Correcto |

Las nueve pruebas de `DurationRuleTests` finalizaron correctamente, sin errores.

La reserva heredada de seis horas se insertó directamente en la base de datos de pruebas para comprobar que el sistema impide aprobarla y conserva su estado `Pendiente`.

La regla se implementó en la lógica de negocio. No requirió modificaciones en las entidades, columnas, relaciones, migraciones ni contratos de datos.

## 4. Validación de OpenSpec inicial

Comando ejecutado:

```bash
openspec validate initial-system
```

Resultado: validación sin errores. Se emiten avisos porque las especificaciones están redactadas en español y usan `DEBE` en lugar de `SHALL`/`MUST`; son los avisos esperados y no afectan la validación.

Los artefactos del cambio inicial (`proposal.md`, ocho especificaciones, `design.md` y `tasks.md`) corresponden a la implementación inicial.

## 5. Cambio reservation-max-duration

Se implementó el cambio independiente `reservation-max-duration` para establecer la duración máxima de cuatro horas consecutivas por reserva.

El cambio contempla:

- Validación de duración al crear solicitudes.
- Revalidación de duración al aprobar solicitudes.
- Validación de franjas horarias en las consultas de disponibilidad.
- Rechazo de reservas heredadas que superen el límite.
- Actualización de la documentación del sistema.

Las nueve pruebas específicas del cambio finalizaron correctamente.

No se realizaron modificaciones en el esquema de la base de datos ni se generaron migraciones adicionales.
