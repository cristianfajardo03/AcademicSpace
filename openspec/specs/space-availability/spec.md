# space-availability Specification

## Purpose

Permite consultar qué espacios académicos están disponibles y qué franjas horarias están libres antes de crear una solicitud de reserva.

## Requirements

### Requirement: Consulta de disponibilidad de un espacio

Un usuario autenticado DEBE poder consultar la disponibilidad de un espacio para una fecha y una franja horaria. El sistema DEBE indicar si el espacio está disponible e informar las franjas ocupadas que se superponen con la consulta, sin revelar la identidad de los solicitantes.

#### Scenario: Espacio disponible

- **WHEN** un usuario consulta un espacio activo para una fecha y franja horaria sin reservas pendientes ni aprobadas que se superpongan
- **THEN** el sistema indica que el espacio está disponible y no reporta ocupaciones en esa franja

#### Scenario: Espacio ocupado parcialmente

- **WHEN** un usuario consulta un espacio activo para una franja que se superpone con una reserva pendiente o aprobada
- **THEN** el sistema indica que el espacio no está disponible y muestra la franja ocupada sin datos personales del solicitante

#### Scenario: Espacio inactivo

- **WHEN** un usuario consulta la disponibilidad de un espacio inactivo
- **THEN** el sistema indica que el espacio no está disponible para reservas

#### Scenario: Espacio inexistente

- **WHEN** un usuario consulta la disponibilidad de un identificador de espacio que no existe
- **THEN** el sistema responde que el espacio no fue encontrado

### Requirement: Consulta de espacios disponibles

Un usuario autenticado DEBE poder consultar la lista de espacios activos disponibles para una fecha y franja horaria, con filtro opcional por tipo de espacio. La lista DEBE excluir los espacios con reservas pendientes o aprobadas que se superpongan.

#### Scenario: Lista de espacios disponibles

- **WHEN** un usuario consulta los espacios disponibles para una fecha y franja horaria
- **THEN** el sistema devuelve los espacios activos sin reservas pendientes ni aprobadas superpuestas

#### Scenario: Filtro por tipo de espacio

- **WHEN** un usuario consulta los espacios disponibles indicando el tipo laboratorio
- **THEN** el sistema devuelve únicamente laboratorios activos disponibles en la franja consultada

#### Scenario: Sin espacios disponibles

- **WHEN** todos los espacios activos tienen reservas pendientes o aprobadas que se superponen con la franja consultada
- **THEN** el sistema devuelve una lista vacía e indica que no hay espacios disponibles

### Requirement: Criterio de ocupación

El sistema DEBE considerar ocupada una franja cuando exista una reserva pendiente o aprobada del mismo espacio cuyos horarios se superpongan. Las reservas rechazadas o canceladas NO DEBEN ocupar la franja.

#### Scenario: Reserva pendiente ocupa la franja

- **WHEN** existe una reserva pendiente que se superpone con la franja consultada
- **THEN** el sistema considera el espacio no disponible en esa franja

#### Scenario: Reserva aprobada ocupa la franja

- **WHEN** existe una reserva aprobada que se superpone con la franja consultada
- **THEN** el sistema considera el espacio no disponible en esa franja

#### Scenario: Reserva cancelada libera la franja

- **WHEN** la única reserva que se superponía con la franja consultada está cancelada o rechazada
- **THEN** el sistema considera el espacio disponible en esa franja

### Requirement: Validación de la consulta de disponibilidad

La consulta de disponibilidad DEBE exigir una fecha y horas de inicio y fin válidas, con la hora de fin posterior a la hora de inicio. Una consulta inválida DEBE rechazarse sin devolver resultados.

#### Scenario: Franja horaria inválida

- **WHEN** un usuario consulta la disponibilidad con una hora de fin anterior o igual a la hora de inicio
- **THEN** el sistema rechaza la consulta e indica que la franja horaria es inválida

#### Scenario: Fecha u hora en el pasado

- **WHEN** un usuario consulta la disponibilidad para una fecha y hora ya transcurridas
- **THEN** el sistema rechaza la consulta e indica que la fecha debe ser futura
