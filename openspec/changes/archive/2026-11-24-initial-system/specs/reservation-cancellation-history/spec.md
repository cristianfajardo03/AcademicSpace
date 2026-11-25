# Spec Delta

## Purpose

Permite al solicitante cancelar sus reservas y consultar el historial de solicitudes y reservas según su rol.

## ADDED Requirements

### Requirement: Cancelación de reservas por el solicitante

El usuario solicitante DEBE poder cancelar sus propias solicitudes en estado pendiente o aprobada. La cancelación DEBE cambiar el estado a cancelada y registrar la fecha de cancelación. Una solicitud rechazada o ya cancelada NO DEBE poder cancelarse.

#### Scenario: Cancelación de una solicitud pendiente

- **WHEN** un solicitante cancela una solicitud propia en estado pendiente
- **THEN** el sistema cambia el estado a cancelada y registra la fecha de cancelación

#### Scenario: Cancelación de una reserva aprobada

- **WHEN** un solicitante cancela una reserva propia en estado aprobada
- **THEN** el sistema cambia el estado a cancelada, registra la fecha y libera la franja para nuevas solicitudes

#### Scenario: Cancelación de una solicitud ya finalizada

- **WHEN** un solicitante intenta cancelar una solicitud propia rechazada o ya cancelada
- **THEN** el sistema rechaza la operación e indica que el estado actual no permite la cancelación

#### Scenario: Cancelación de una reserva ajena

- **WHEN** un solicitante intenta cancelar una solicitud creada por otro usuario
- **THEN** el sistema rechaza la operación por permisos insuficientes

### Requirement: Historial del solicitante

El usuario solicitante DEBE poder consultar el historial de todas sus solicitudes y reservas, incluidos los estados pendiente, aprobada, rechazada y cancelada. El historial DEBE mostrar espacio, fecha, horario, estado y las fechas de solicitud, decisión o cancelación cuando correspondan.

#### Scenario: Historial con todos los estados

- **WHEN** un solicitante consulta su historial y tiene solicitudes en distintos estados
- **THEN** el sistema devuelve todas sus solicitudes con el estado y las fechas registradas para cada una

#### Scenario: Historial sin registros

- **WHEN** un solicitante sin solicitudes consulta su historial
- **THEN** el sistema devuelve una lista vacía

### Requirement: Historial para el responsable de aprobación

El responsable de aprobación DEBE poder consultar las reservas aprobadas y el historial de solicitudes del sistema, con espacio, fecha, horario, solicitante, estado y fechas de decisión.

#### Scenario: Consulta de reservas aprobadas

- **WHEN** el responsable de aprobación consulta las reservas aprobadas
- **THEN** el sistema devuelve las reservas en estado aprobada con su espacio, fecha, horario y solicitante

#### Scenario: Consulta del historial general

- **WHEN** el responsable de aprobación consulta el historial de solicitudes
- **THEN** el sistema devuelve las solicitudes de todos los usuarios con su estado y fechas registradas

### Requirement: Historial para el administrador

El administrador DEBE poder consultar el historial completo de solicitudes y reservas del sistema, con espacio, fecha, horario, solicitante, estado y fechas de decisión o cancelación.

#### Scenario: Administrador consulta el historial completo

- **WHEN** un administrador consulta el historial del sistema
- **THEN** el sistema devuelve todas las solicitudes y reservas registradas con su estado y fechas

### Requirement: Conservación del historial

El sistema NO DEBE eliminar solicitudes ni reservas al rechazarlas o cancelarlas. Los registros DEBEN permanecer consultables en el historial con su estado final.

#### Scenario: Conservación tras la cancelación

- **WHEN** una reserva es cancelada
- **THEN** el sistema conserva el registro en el historial de forma consultable con el estado cancelada
