# Proposal

## Why

La institución gestiona hoy las reservas de espacios académicos mediante correos electrónicos y hojas de cálculo, lo que provoca solicitudes duplicadas, conflictos de horario, dificultad para consultar disponibilidad y demoras en las aprobaciones. AcademicSpace centraliza esos procesos en una aplicación web con reglas de negocio verificables.

## What Changes

- Se incorpora la base funcional del sistema AcademicSpace: autenticación, administración de usuarios y roles, gestión de espacios académicos, solicitudes de reserva, consulta de disponibilidad, prevención de reservas superpuestas, aprobaciones/rechazos, cancelaciones e historial.
- Se establecen tres roles con permisos diferenciados: administrador, usuario solicitante y responsable de aprobación.
- Se definen los estados de una reserva y las transiciones permitidas entre solicitud, aprobación, rechazo y cancelación.
- Se define la regla de conflicto: dos reservas del mismo espacio se consideran incompatibles si sus intervalos se superponen y al menos una está pendiente o aprobada.
- Se define la arquitectura de monolito modular por capas con .NET 8 Web API, React con TypeScript, MySQL, Entity Framework Core y pruebas con xUnit.
- **Fuera de alcance de este cambio**: la regla de máximo cuatro horas consecutivas (se gestionará como cambio independiente), pagos, integraciones con sistemas académicos externos, aplicación móvil, notificaciones por SMS y cualquier funcionalidad no descrita en `PLAN_INGENIERIA.md`.

## Capabilities

### New Capabilities

- `access-control`: autenticación de usuarios y autorización de operaciones según el rol (administrador, usuario solicitante, responsable de aprobación).
- `user-management`: registro, consulta, actualización y administración de usuarios, incluida la asignación de roles.
- `academic-spaces`: registro, consulta, modificación y desactivación de laboratorios, auditorios y salas de cómputo.
- `reservation-requests`: creación y consulta de solicitudes de reserva con su estado.
- `space-availability`: consulta de espacios y franjas horarias disponibles para una fecha determinada.
- `reservation-conflicts`: prevención de reservas superpuestas para un mismo espacio.
- `reservation-approvals`: aprobación o rechazo de solicitudes pendientes por el responsable de aprobación.
- `reservation-cancellation-history`: cancelación de reservas y consulta del historial de solicitudes y reservas.

### Modified Capabilities

Ninguna. El proyecto no cuenta todavía con especificaciones existentes.

## Impact

- **Nuevo backend**: solución .NET 8 Web API organizada en módulos y capas (presentación/API, lógica de negocio, persistencia), con autenticación, autorización por roles y validaciones transaccionales.
- **Nueva base de datos MySQL**: entidades Usuario, Rol, Espacio y Reserva, con relaciones y restricciones que soportan las reglas de negocio.
- **Nuevo frontend**: aplicación React con TypeScript que consume la API para los flujos de cada rol.
- **Nuevas pruebas**: proyectos xUnit para reglas de negocio (conflictos, estados, permisos) y pruebas de integración con la base de datos.
- **Dependencias**: .NET 8, Entity Framework Core con proveedor MySQL, React, TypeScript y xUnit, según `PLAN_INGENIERIA.md`.
- **Sin impacto** en sistemas externos: no hay integraciones, pagos ni aplicaciones móviles en el alcance.
