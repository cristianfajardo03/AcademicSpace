# Spec Delta

## Purpose

Impide que un mismo espacio académico tenga reservas superpuestas, incluso cuando las solicitudes se envían al mismo tiempo.

## ADDED Requirements

### Requirement: Rechazo de reservas superpuestas

El sistema DEBE rechazar la creación de una solicitud cuando exista otra solicitud pendiente o aprobada del mismo espacio cuyos horarios se superpongan. Dos intervalos se superponen cuando el inicio de uno es anterior al fin del otro y su fin es posterior al inicio del otro. La solicitud rechazada NO DEBE registrarse.

#### Scenario: Superposición parcial al inicio

- **WHEN** un usuario solicita un espacio para las 10:00–12:00 y existe una reserva pendiente para las 09:00–11:00 del mismo espacio y fecha
- **THEN** el sistema rechaza la nueva solicitud e indica el conflicto de horario

#### Scenario: Superposición total

- **WHEN** un usuario solicita un espacio para las 10:00–11:00 y existe una reserva aprobada para las 09:00–12:00 del mismo espacio y fecha
- **THEN** el sistema rechaza la nueva solicitud e indica el conflicto de horario

#### Scenario: Reserva de otro espacio en el mismo horario

- **WHEN** dos solicitudes usan el mismo horario pero espacios diferentes
- **THEN** el sistema acepta ambas solicitudes porque no existe conflicto de espacio

### Requirement: Intervalos adyacentes permitidos

El sistema DEBE permitir reservas consecutivas del mismo espacio cuando el fin de una coincide con el inicio de la otra, sin superposición.

#### Scenario: Reservas consecutivas

- **WHEN** existe una reserva de 09:00–10:00 y se solicita el mismo espacio para las 10:00–11:00 del mismo día
- **THEN** el sistema acepta la nueva solicitud porque los intervalos no se superponen

### Requirement: Prevención de conflictos bajo solicitudes simultáneas

El sistema DEBE garantizar que dos solicitudes superpuestas para el mismo espacio no puedan quedar registradas simultáneamente como pendientes o aprobadas, aunque se envíen en el mismo instante. Como máximo una DEBE completarse; la otra DEBE rechazarse por conflicto.

#### Scenario: Dos solicitudes simultáneas superpuestas

- **WHEN** dos usuarios envían al mismo tiempo solicitudes superpuestas para el mismo espacio y fecha
- **THEN** el sistema registra como máximo una solicitud y rechaza la otra por conflicto de horario

#### Scenario: Reintento tras un rechazo por conflicto

- **WHEN** una solicitud fue rechazada por conflicto y el usuario reintenta con una franja libre
- **THEN** el sistema acepta la nueva solicitud si ya no existe una reserva pendiente o aprobada superpuesta

### Requirement: Revalidación del conflicto al aprobar

Antes de aprobar una solicitud, el sistema DEBE verificar que no exista otra reserva pendiente o aprobada superpuesta del mismo espacio. Si existe el conflicto, la aprobación DEBE rechazarse y la solicitud DEBE conservar su estado pendiente.

#### Scenario: Aprobación sin conflicto

- **WHEN** el responsable de aprobación aprueba una solicitud pendiente que no se superpone con otra reserva pendiente o aprobada
- **THEN** el sistema aprueba la solicitud y actualiza su estado

#### Scenario: Aprobación con conflicto

- **WHEN** el responsable de aprobación intenta aprobar una solicitud que se superpone con otra reserva pendiente o aprobada del mismo espacio
- **THEN** el sistema rechaza la aprobación, informa el conflicto y mantiene la solicitud en estado pendiente

### Requirement: Las reservas no vigentes no bloquean

Las solicitudes rechazadas o canceladas NO DEBEN considerarse para detectar conflictos ni impedir nuevas reservas.

#### Scenario: Nueva reserva sobre una franja liberada

- **WHEN** una reserva fue cancelada y otro usuario solicita el mismo espacio en la misma franja
- **THEN** el sistema acepta la nueva solicitud porque la reserva cancelada no bloquea el horario
