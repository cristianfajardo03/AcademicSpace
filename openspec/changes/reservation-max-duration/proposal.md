# Proposal

## Why

AcademicSpace hoy solo impide reservas superpuestas: no existe ningún límite de duración, por lo que un solicitante puede bloquear un laboratorio, auditorio o sala de cómputo durante una jornada completa con una sola solicitud. La regla institucional exige que cada reserva dure como máximo cuatro horas consecutivas, de modo que los espacios roten y queden disponibles para más grupos.

Este límite se dejó explícitamente fuera del cambio `initial-system` y quedó documentado como pendiente; ahora se incorpora como un cambio acotado.

## What Changes

- Se agrega una regla única de duración máxima: una reserva **NO DEBE** superar las cuatro horas consecutivas.
- Una duración de **exactamente cuatro horas** se permite; cualquier duración mayor se rechaza con un mensaje que indica el límite.
- La validación se aplica en **tres puntos**: creación de solicitudes, aprobación de solicitudes y consulta de disponibilidad.
- La aprobación revalida la regla para impedir aprobar solicitudes pendientes que la incumplan (por ejemplo, datos heredados creados antes de este cambio).
- Se reescriben las pruebas que hoy afirman lo contrario: `DurationRuleTests` verifica que una reserva de seis horas es aceptada, y debe pasar a verificar el rechazo.
- Se actualiza la documentación que declara la regla como fuera de alcance (`README.md`, `docs/api.md`, `docs/modelo-datos.md`, `docs/verificacion.md`).
- **Sin cambios** de esquema, migraciones, dependencias, contratos de respuesta ni interfaz de usuario: el frontend ya muestra el mensaje de error devuelto por la API.

## Capabilities

### New Capabilities

Ninguna. La regla se incorpora a capacidades existentes porque cada una ya es dueña de su propia validación.

### Modified Capabilities

- `reservation-requests`: la creación de una solicitud exige que la duración no supere cuatro horas consecutivas.
- `reservation-approvals`: la aprobación revalida la duración máxima antes de cambiar el estado.
- `space-availability`: la consulta de disponibilidad valida la duración de la franja solicitada con la misma regla.

## Impact

- **Código backend:** `Modules/Reservations/Services/ReservationRules.cs` (regla y constante compartida), `Modules/Reservations/Services/ReservationService.cs` (`CreateAsync`, `ApproveAsync`), `Modules/Availability/Services/AvailabilityService.cs` (`Validate`).
- **API:** `POST /api/reservations` y `GET /api/availability/spaces`, `GET /api/availability/spaces/{id}` responden `400` con el motivo cuando la franja supera cuatro horas; `POST /api/reservations/{id}/approve` responde `409` si la solicitud pendiente supera el límite y conserva el estado pendiente.
- **Pruebas:** `tests/AcademicSpace.Tests/DurationRuleTests.cs` se reescribe; se agregan casos de aprobación y de disponibilidad.
- **Documentación:** `README.md`, `docs/api.md`, `docs/modelo-datos.md`, `docs/verificacion.md`.
- **Sin impacto** en base de datos, migraciones, autenticación, usuarios, espacios, conflictos de superposición, cancelaciones, historial ni frontend.
