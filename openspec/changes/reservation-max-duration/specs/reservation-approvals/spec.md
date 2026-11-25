# Spec Delta

## MODIFIED Requirements

### Requirement: Aprobación de solicitudes

El responsable de aprobación DEBE poder aprobar una solicitud pendiente. Antes de aprobarla DEBE verificarse que su duración no supere cuatro horas consecutivas; si la supera, la aprobación DEBE rechazarse con el motivo y la solicitud DEBE conservar su estado pendiente. La aprobación DEBE cambiar el estado a aprobada y registrar al responsable y la fecha de decisión. Una solicitud que no está pendiente NO DEBE poder aprobarse.

#### Scenario: Aprobación exitosa

- **WHEN** el responsable de aprobación aprueba una solicitud pendiente sin conflictos de horario
- **THEN** el sistema cambia el estado a aprobada y registra el responsable y la fecha de la decisión

#### Scenario: Aprobación de una solicitud ya decidida

- **WHEN** el responsable de aprobación intenta aprobar una solicitud rechazada, cancelada o ya aprobada
- **THEN** el sistema rechaza la operación e indica que la solicitud ya no está pendiente

#### Scenario: Usuario sin permiso de aprobación

- **WHEN** un usuario solicitante o un administrador intenta aprobar una solicitud
- **THEN** el sistema rechaza la operación por permisos insuficientes

#### Scenario: Aprobación de una solicitud que supera cuatro horas

- **WHEN** el responsable de aprobación intenta aprobar una solicitud pendiente cuya duración supera cuatro horas consecutivas
- **THEN** el sistema rechaza la aprobación, indica que el máximo es de cuatro horas consecutivas y mantiene la solicitud en estado pendiente
