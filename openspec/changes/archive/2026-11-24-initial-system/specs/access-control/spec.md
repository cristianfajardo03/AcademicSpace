# Spec Delta

## Purpose

Controla el acceso al sistema mediante credenciales y restringe cada operación según el rol del usuario autenticado.

## ADDED Requirements

### Requirement: Inicio de sesión con credenciales

El sistema DEBE permitir iniciar sesión con correo electrónico y contraseña. Solo los usuarios activos con credenciales válidas obtienen una sesión autenticada. Las credenciales inválidas o las cuentas inactivas DEBEN rechazarse con un mensaje genérico que no revele si el correo existe.

#### Scenario: Credenciales válidas de un usuario activo

- **WHEN** un usuario activo envía su correo y contraseña correctos
- **THEN** el sistema inicia una sesión autenticada y permite acceder a las operaciones habilitadas para su rol

#### Scenario: Contraseña incorrecta

- **WHEN** un usuario envía un correo registrado con una contraseña incorrecta
- **THEN** el sistema rechaza el inicio de sesión con un mensaje genérico de credenciales inválidas y no inicia sesión

#### Scenario: Cuenta inactiva

- **WHEN** un usuario con cuenta desactivada envía credenciales correctas
- **THEN** el sistema rechaza el inicio de sesión e indica que la cuenta no está habilitada

### Requirement: Cierre de sesión

El sistema DEBE permitir a un usuario autenticado cerrar su sesión. Después del cierre, la sesión cerrada DEBE rechazarse en las operaciones que requieren autenticación.

#### Scenario: Cierre de sesión exitoso

- **WHEN** un usuario autenticado cierra su sesión
- **THEN** el sistema finaliza la sesión y las siguientes operaciones con esa sesión se rechazan como no autenticadas

### Requirement: Autenticación obligatoria

Toda operación del sistema, excepto el inicio de sesión, DEBE requerir una sesión autenticada. Las solicitudes sin sesión válida DEBEN rechazarse sin ejecutar la operación.

#### Scenario: Operación sin sesión

- **WHEN** se invoca una operación protegida sin una sesión válida
- **THEN** el sistema rechaza la solicitud como no autenticada y no modifica ningún dato

#### Scenario: Sesión válida

- **WHEN** un usuario autenticado invoca una operación permitida para su rol
- **THEN** el sistema ejecuta la operación y devuelve el resultado correspondiente

### Requirement: Roles del sistema

El sistema DEBE manejar exactamente tres roles: administrador, usuario solicitante y responsable de aprobación. Cada usuario DEBE tener asignado un único rol.

#### Scenario: Usuario con un solo rol

- **WHEN** se consulta un usuario registrado
- **THEN** el sistema muestra uno de los tres roles definidos y no una combinación de roles

### Requirement: Autorización por rol

El sistema DEBE autorizar cada operación según el rol del usuario. Un usuario sin el rol requerido DEBE recibir un rechazo por permisos insuficientes sin que se ejecute la operación.

#### Scenario: Administrador administra usuarios y espacios

- **WHEN** un administrador intenta registrar usuarios o gestionar espacios académicos
- **THEN** el sistema permite la operación

#### Scenario: Solicitante crea y cancela sus reservas

- **WHEN** un usuario solicitante crea una solicitud de reserva o cancela una reserva propia
- **THEN** el sistema permite la operación

#### Scenario: Responsable de aprobación aprueba o rechaza

- **WHEN** un responsable de aprobación aprueba o rechaza una solicitud pendiente
- **THEN** el sistema permite la operación

#### Scenario: Operación fuera del rol

- **WHEN** un usuario solicitante intenta aprobar una solicitud o un responsable de aprobación intenta gestionar usuarios
- **THEN** el sistema rechaza la operación por permisos insuficientes

### Requirement: Protección de credenciales

El sistema DEBE almacenar las contraseñas de forma irreversible y NUNCA DEBE devolverlas en respuestas, consultas ni mensajes de error.

#### Scenario: Consulta de usuario sin contraseña

- **WHEN** se consulta la información de un usuario
- **THEN** la respuesta incluye sus datos de perfil y rol, pero no su contraseña ni su representación almacenada
