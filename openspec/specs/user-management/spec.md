# user-management Specification

## Purpose

Permite registrar, consultar, actualizar y administrar las cuentas de usuario del sistema, incluida la asignación de un rol.

## Requirements

### Requirement: Registro de usuarios

El administrador DEBE poder registrar un usuario con nombre, correo electrónico, contraseña y rol. El correo DEBE ser único en el sistema y la contraseña DEBE tener al menos ocho caracteres. Un registro válido crea una cuenta activa con el rol indicado.

#### Scenario: Registro válido

- **WHEN** el administrador registra un usuario con nombre, correo único, contraseña de al menos ocho caracteres y uno de los tres roles
- **THEN** el sistema crea el usuario activo, asigna el rol indicado y confirma el registro sin devolver la contraseña

#### Scenario: Correo duplicado

- **WHEN** el administrador intenta registrar un usuario con un correo que ya pertenece a otra cuenta
- **THEN** el sistema rechaza el registro e indica que el correo ya está en uso

#### Scenario: Datos inválidos

- **WHEN** el administrador intenta registrar un usuario sin nombre, con correo inválido, con contraseña de menos de ocho caracteres o con un rol inexistente
- **THEN** el sistema rechaza el registro e indica los campos inválidos sin crear la cuenta

### Requirement: Consulta de usuarios

El administrador DEBE poder consultar la lista de usuarios y el detalle de cualquier usuario. Un usuario autenticado DEBE poder consultar su propio perfil. La información consultada DEBE incluir nombre, correo, rol y estado, sin contraseña.

#### Scenario: Administrador consulta todos los usuarios

- **WHEN** un administrador solicita la lista de usuarios
- **THEN** el sistema devuelve todos los usuarios registrados, activos e inactivos, con su rol y estado

#### Scenario: Usuario consulta su perfil

- **WHEN** un usuario autenticado solicita su propio perfil
- **THEN** el sistema devuelve sus datos de nombre, correo, rol y estado, sin contraseña

#### Scenario: Consulta de otro usuario sin permiso

- **WHEN** un usuario solicitante intenta consultar el perfil de otro usuario
- **THEN** el sistema rechaza la consulta por permisos insuficientes

### Requirement: Actualización de datos de usuario

El administrador DEBE poder actualizar el nombre, correo, rol y estado de cualquier usuario. Un usuario autenticado DEBE poder actualizar su propio nombre y correo, pero no su rol ni su estado. Todo correo actualizado DEBE seguir siendo único.

#### Scenario: Actualización por el administrador

- **WHEN** el administrador actualiza el nombre, correo o rol de un usuario con datos válidos
- **THEN** el sistema guarda los cambios y los refleja en la siguiente consulta

#### Scenario: Usuario actualiza su perfil

- **WHEN** un usuario autenticado actualiza su propio nombre o correo con datos válidos
- **THEN** el sistema guarda los cambios y mantiene su rol y estado sin modificaciones

#### Scenario: Intento de cambiar el propio rol

- **WHEN** un usuario autenticado intenta modificar su propio rol o estado
- **THEN** el sistema rechaza la modificación y conserva los valores anteriores

#### Scenario: Correo ya utilizado en una actualización

- **WHEN** una actualización intenta asignar un correo que pertenece a otra cuenta
- **THEN** el sistema rechaza la actualización e indica que el correo ya está en uso

### Requirement: Administración del estado de usuario

El administrador DEBE poder desactivar y reactivar usuarios. Un usuario desactivado no DEBE poder iniciar sesión ni ejecutar operaciones autenticadas.

#### Scenario: Desactivación de usuario

- **WHEN** el administrador desactiva un usuario activo
- **THEN** el sistema marca la cuenta como inactiva y rechaza sus intentos posteriores de inicio de sesión

#### Scenario: Reactivación de usuario

- **WHEN** el administrador reactiva un usuario inactivo
- **THEN** el sistema marca la cuenta como activa y el usuario puede iniciar sesión nuevamente
