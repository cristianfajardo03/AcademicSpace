# Spec Delta

## Purpose

Permite a los usuarios solicitantes crear solicitudes de reserva de espacios académicos y consultar su estado a lo largo de su ciclo de vida.

## ADDED Requirements

### Requirement: Creación de solicitudes de reserva

Un usuario autenticado con rol usuario solicitante DEBE poder crear una solicitud indicando espacio, fecha, hora de inicio, hora de fin y motivo. La fecha y las horas DEBEN ser válidas, la hora de fin DEBE ser posterior a la hora de inicio y el espacio DEBE estar activo. La solicitud DEBE crearse en estado pendiente y asociada al usuario que la crea.

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

### Requirement: Consulta de solicitudes de reserva

El usuario solicitante DEBE poder consultar sus propias solicitudes. El responsable de aprobación y el administrador DEBEN poder consultar todas las solicitudes. La consulta DEBE mostrar espacio, fecha, horario, motivo, estado y las fechas de solicitud, decisión o cancelación cuando correspondan.

#### Scenario: Solicitante consulta sus solicitudes

- **WHEN** un usuario solicitante solicita la lista de sus solicitudes
- **THEN** el sistema devuelve únicamente las solicitudes creadas por ese usuario con su estado actual

#### Scenario: Solicitante consulta una solicitud ajena

- **WHEN** un usuario solicitante intenta consultar el detalle de una solicitud creada por otro usuario
- **THEN** el sistema rechaza la consulta por permisos insuficientes

#### Scenario: Responsable de aprobación consulta solicitudes

- **WHEN** un responsable de aprobación solicita la lista de solicitudes
- **THEN** el sistema devuelve las solicitudes de todos los usuarios con su estado actual

#### Scenario: Administrador consulta solicitudes

- **WHEN** un administrador solicita la lista de solicitudes
- **THEN** el sistema devuelve las solicitudes de todos los usuarios con su estado actual

### Requirement: Ciclo de vida de la solicitud

Una solicitud DEBE encontrarse en uno de cuatro estados: pendiente, aprobada, rechazada o cancelada. Solo se permiten las transiciones pendiente a aprobada, pendiente a rechazada, pendiente a cancelada y aprobada a cancelada. Los estados rechazada y cancelada son finales.

#### Scenario: Solicitud recién creada

- **WHEN** se crea una solicitud de reserva
- **THEN** su estado inicial es pendiente

#### Scenario: Transición no permitida

- **WHEN** se intenta aprobar, rechazar o cancelar una solicitud que ya está rechazada o cancelada
- **THEN** el sistema rechaza la operación e indica que el estado actual no permite esa transición

### Requirement: Consulta del estado de una solicitud

El solicitante DEBE poder consultar el estado actual de cada una de sus solicitudes, incluida la causa cuando haya sido rechazada o cancelada.

#### Scenario: Consulta de una solicitud rechazada

- **WHEN** el solicitante consulta una solicitud rechazada
- **THEN** el sistema muestra el estado rechazada y el motivo registrado por el responsable de aprobación

#### Scenario: Consulta de una solicitud cancelada

- **WHEN** el solicitante consulta una solicitud cancelada
- **THEN** el sistema muestra el estado cancelada y la fecha de cancelación
