# Spec Delta

## MODIFIED Requirements

### Requirement: Creación de solicitudes de reserva

Un usuario autenticado con rol usuario solicitante DEBE poder crear una solicitud indicando espacio, fecha, hora de inicio, hora de fin y motivo. La fecha y las horas DEBEN ser válidas, la hora de fin DEBE ser posterior a la hora de inicio y el espacio DEBE estar activo. La duración DEBE ser como máximo de cuatro horas consecutivas y una duración de exactamente cuatro horas DEBE aceptarse. La solicitud DEBE crearse en estado pendiente y asociada al usuario que la crea.

#### Scenario: Creación válida

- **WHEN** un usuario solicitante envía una solicitud con un espacio activo, fecha futura, hora de fin posterior a la hora de inicio y motivo
- **THEN** el sistema crea la solicitud en estado pendiente, asociada al solicitante, y la devuelve con su identificador

#### Scenario: Hora de fin anterior o igual a la de inicio

- **WHEN** un usuario solicitante envía una solicitud cuya hora de fin es anterior o igual a la hora de inicio
- **THEN** el sistema rechaza la solicitud e indica que el intervalo de horario es inválido

#### Scenario: Fecha u hora en el pasado

- **WHEN** un usuario solicitante envía una solicitud para una fecha y hora ya transcurridas
- **THEN** el sistema rechaza la solicitud e indica que la reserva debe ser futura

#### Scenario: Campos obligatorios faltantes

- **WHEN** un usuario solicitante envía una solicitud sin espacio, sin fecha, sin horas o sin motivo
- **THEN** el sistema rechaza la solicitud e indica los campos obligatorios faltantes

#### Scenario: Rol sin permiso para solicitar reservas

- **WHEN** un usuario con rol administrador o responsable de aprobación intenta crear una solicitud de reserva
- **THEN** el sistema rechaza la operación por permisos insuficientes

#### Scenario: Duración de exactamente cuatro horas

- **WHEN** un usuario solicitante envía una solicitud con una duración de exactamente cuatro horas consecutivas
- **THEN** el sistema crea la solicitud en estado pendiente

#### Scenario: Duración menor de cuatro horas

- **WHEN** un usuario solicitante envía una solicitud con una duración menor de cuatro horas consecutivas
- **THEN** el sistema crea la solicitud en estado pendiente

#### Scenario: Duración mayor de cuatro horas

- **WHEN** un usuario solicitante envía una solicitud con una duración mayor de cuatro horas consecutivas
- **THEN** el sistema rechaza la solicitud, indica que el máximo es de cuatro horas consecutivas y no registra la solicitud
