# Spec Delta

## MODIFIED Requirements

### Requirement: Validación de la consulta de disponibilidad

La consulta de disponibilidad DEBE exigir una fecha y horas de inicio y fin válidas, con la hora de fin posterior a la hora de inicio, y la franja solicitada NO DEBE superar cuatro horas consecutivas. Una consulta inválida DEBE rechazarse sin devolver resultados.

#### Scenario: Franja horaria inválida

- **WHEN** un usuario consulta la disponibilidad con una hora de fin anterior o igual a la hora de inicio
- **THEN** el sistema rechaza la consulta e indica que la franja horaria es inválida

#### Scenario: Fecha u hora en el pasado

- **WHEN** un usuario consulta la disponibilidad para una fecha y hora ya transcurridas
- **THEN** el sistema rechaza la consulta e indica que la fecha debe ser futura

#### Scenario: Franja de exactamente cuatro horas

- **WHEN** un usuario consulta la disponibilidad para una franja de exactamente cuatro horas consecutivas
- **THEN** el sistema acepta la consulta y devuelve el resultado de disponibilidad correspondiente

#### Scenario: Franja menor de cuatro horas

- **WHEN** un usuario consulta la disponibilidad para una franja menor de cuatro horas consecutivas
- **THEN** el sistema acepta la consulta y devuelve el resultado de disponibilidad correspondiente

#### Scenario: Franja mayor de cuatro horas

- **WHEN** un usuario consulta la disponibilidad para una franja mayor de cuatro horas consecutivas
- **THEN** el sistema rechaza la consulta, indica que el máximo es de cuatro horas consecutivas y no devuelve resultados
