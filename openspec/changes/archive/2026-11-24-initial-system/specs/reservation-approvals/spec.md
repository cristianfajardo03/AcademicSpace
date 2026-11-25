# Spec Delta

## Purpose

Permite al responsable de aprobación revisar las solicitudes pendientes y decidir si se aprueban o se rechazan.

## ADDED Requirements

### Requirement: Consulta de solicitudes pendientes

El responsable de aprobación DEBE poder consultar las solicitudes en estado pendiente, con espacio, fecha, horario, motivo y solicitante, ordenadas de la más antigua a la más reciente.

#### Scenario: Lista de solicitudes pendientes

- **WHEN** el responsable de aprobación solicita las solicitudes pendientes
- **THEN** el sistema devuelve únicamente las solicitudes en estado pendiente con sus datos de espacio, fecha, horario, motivo y solicitante

#### Scenario: Sin solicitudes pendientes

- **WHEN** no existen solicitudes en estado pendiente
- **THEN** el sistema devuelve una lista vacía

### Requirement: Aprobación de solicitudes

El responsable de aprobación DEBE poder aprobar una solicitud pendiente. La aprobación DEBE cambiar el estado a aprobada y registrar al responsable y la fecha de decisión. Una solicitud que no está pendiente NO DEBE poder aprobarse.

#### Scenario: Aprobación exitosa

- **WHEN** el responsable de aprobación aprueba una solicitud pendiente sin conflictos de horario
- **THEN** el sistema cambia el estado a aprobada y registra el responsable y la fecha de la decisión

#### Scenario: Aprobación de una solicitud ya decidida

- **WHEN** el responsable de aprobación intenta aprobar una solicitud rechazada, cancelada o ya aprobada
- **THEN** el sistema rechaza la operación e indica que la solicitud ya no está pendiente

#### Scenario: Usuario sin permiso de aprobación

- **WHEN** un usuario solicitante o un administrador intenta aprobar una solicitud
- **THEN** el sistema rechaza la operación por permisos insuficientes

### Requirement: Rechazo de solicitudes

El responsable de aprobación DEBE poder rechazar una solicitud pendiente indicando un motivo. El rechazo DEBE cambiar el estado a rechazada y registrar al responsable, la fecha de decisión y el motivo. Una solicitud sin motivo NO DEBE rechazarse.

#### Scenario: Rechazo exitoso

- **WHEN** el responsable de aprobación rechaza una solicitud pendiente con un motivo
- **THEN** el sistema cambia el estado a rechazada y registra el responsable, la fecha y el motivo del rechazo

#### Scenario: Rechazo sin motivo

- **WHEN** el responsable de aprobación intenta rechazar una solicitud sin indicar motivo
- **THEN** el sistema rechaza la operación e indica que el motivo es obligatorio, sin cambiar el estado de la solicitud

#### Scenario: Rechazo de una solicitud ya decidida

- **WHEN** el responsable de aprobación intenta rechazar una solicitud aprobada, cancelada o ya rechazada
- **THEN** el sistema rechaza la operación e indica que la solicitud ya no está pendiente

### Requirement: Trazabilidad de la decisión

Toda aprobación o rechazo DEBE quedar registrado con el responsable que tomó la decisión y la fecha en que se tomó. Esta información DEBE estar disponible al consultar la solicitud y su historial.

#### Scenario: Consulta de una decisión registrada

- **WHEN** se consulta una solicitud aprobada o rechazada
- **THEN** el sistema muestra el estado, el responsable de la decisión y la fecha correspondiente
