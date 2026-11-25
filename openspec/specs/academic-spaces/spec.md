# academic-spaces Specification

## Purpose

Permite administrar los espacios académicos de la universidad —laboratorios, auditorios y salas de cómputo— y controlar cuáles están habilitados para recibir reservas.

## Requirements

### Requirement: Registro de espacios académicos

El administrador DEBE poder registrar un espacio con nombre, tipo, ubicación y capacidad. El tipo DEBE ser laboratorio, auditorio o sala de cómputo. El nombre DEBE ser único y la capacidad DEBE ser mayor que cero.

#### Scenario: Registro válido de un espacio

- **WHEN** el administrador registra un espacio con nombre único, uno de los tres tipos permitidos, ubicación y capacidad mayor que cero
- **THEN** el sistema crea el espacio como activo y lo deja disponible para consultas y solicitudes de reserva

#### Scenario: Tipo de espacio inválido

- **WHEN** el administrador intenta registrar un espacio con un tipo distinto de laboratorio, auditorio o sala de cómputo
- **THEN** el sistema rechaza el registro e indica que el tipo no es válido

#### Scenario: Nombre de espacio duplicado

- **WHEN** el administrador intenta registrar un espacio con un nombre que ya existe
- **THEN** el sistema rechaza el registro e indica que el nombre ya está en uso

#### Scenario: Capacidad inválida

- **WHEN** el administrador intenta registrar un espacio con capacidad cero o negativa
- **THEN** el sistema rechaza el registro e indica que la capacidad debe ser mayor que cero

### Requirement: Consulta de espacios académicos

Los usuarios autenticados DEBEN poder consultar la lista y el detalle de los espacios activos. El administrador DEBE poder consultar también los espacios inactivos. La consulta DEBE incluir nombre, tipo, ubicación, capacidad y estado.

#### Scenario: Consulta de espacios activos

- **WHEN** un usuario autenticado solicita la lista de espacios
- **THEN** el sistema devuelve únicamente los espacios activos con su tipo, ubicación, capacidad y estado

#### Scenario: Administrador consulta espacios inactivos

- **WHEN** un administrador solicita la lista completa de espacios
- **THEN** el sistema devuelve los espacios activos e inactivos con su estado

#### Scenario: Detalle de un espacio inexistente

- **WHEN** se consulta el detalle de un identificador de espacio que no existe
- **THEN** el sistema responde que el espacio no fue encontrado

### Requirement: Modificación de espacios académicos

El administrador DEBE poder modificar el nombre, tipo, ubicación, capacidad y estado de un espacio existente. Las modificaciones DEBEN respetar la unicidad del nombre, los tipos permitidos y la capacidad mayor que cero.

#### Scenario: Modificación válida

- **WHEN** el administrador modifica un espacio con datos válidos
- **THEN** el sistema guarda los cambios y los refleja en la siguiente consulta

#### Scenario: Modificación con nombre duplicado

- **WHEN** el administrador intenta asignar a un espacio el nombre de otro espacio existente
- **THEN** el sistema rechaza la modificación y conserva los datos anteriores

### Requirement: Desactivación de espacios académicos

El administrador DEBE poder desactivar un espacio. Un espacio inactivo NO DEBE aparecer en la consulta de disponibilidad ni aceptar nuevas solicitudes de reserva. Las reservas existentes del espacio NO DEBEN eliminarse ni modificarse automáticamente por la desactivación.

#### Scenario: Desactivación de un espacio

- **WHEN** el administrador desactiva un espacio activo
- **THEN** el sistema marca el espacio como inactivo y deja de ofrecerlo para nuevas reservas

#### Scenario: Solicitud para un espacio inactivo

- **WHEN** un usuario intenta crear una solicitud de reserva para un espacio inactivo
- **THEN** el sistema rechaza la solicitud e indica que el espacio no está disponible

#### Scenario: Reservas existentes tras la desactivación

- **WHEN** un espacio con reservas existentes es desactivado
- **THEN** el sistema conserva dichas reservas y su estado sin cambios automáticos

### Requirement: Eliminación de espacios académicos

El sistema NO DEBE eliminar físicamente espacios académicos. La baja de un espacio DEBE realizarse mediante su desactivación.

#### Scenario: Intento de eliminar un espacio

- **WHEN** el administrador intenta eliminar un espacio del sistema
- **THEN** el sistema no elimina el registro y ofrece desactivarlo en su lugar
