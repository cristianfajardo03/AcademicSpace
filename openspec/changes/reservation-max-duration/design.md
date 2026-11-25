# Design

## Context

AcademicSpace ya valida estructura y superposición en tres puntos: `ReservationService.CreateAsync` (validación previa + transacción con bloqueo del espacio), `ReservationService.ApproveAsync` (revalidación dentro de la transacción) y `AvailabilityService.Validate` (compartida por los dos endpoints de disponibilidad). La regla de superposición vive en `ReservationRules`, un helper estático compartido.

La duración se calcula sobre `DateOnly`/`TimeOnly` locales, que es como se almacenan `Fecha`, `HoraInicio` y `HoraFin` en MySQL (`DATE`/`TIME`). No hay conversión de zona horaria en el dominio.

Ver `proposal.md` para la motivación.

## Goals / Non-Goals

**Goals:**

- Una única definición del límite de cuatro horas, usada por creación, aprobación y disponibilidad.
- Permitir exactamente cuatro horas y rechazar cualquier duración mayor con un mensaje explícito.
- Cubrir con pruebas los tres puntos de validación y los tres casos de duración (menos, exactamente y más de cuatro horas).

**Non-Goals:**

- No se agrega configuración del límite (variable de entorno, tabla o parámetro): el valor es fijo en cuatro horas.
- No se modifica la regla de superposición, los estados, la concurrencia ni el modelo de datos.
- No se agrega validación en el frontend: la API es la fuente de verdad y la interfaz ya muestra el `detail` del error.
- No se corrigen retroactivamente reservas existentes que superen el límite.

## Decisions

### 1. Regla única en `ReservationRules`

Se agrega a `ReservationRules` una constante y un predicado:

```csharp
public static readonly TimeSpan MaxDuracion = TimeSpan.FromHours(4);

public static bool ExceedsMaxDuration(TimeOnly inicio, TimeOnly fin) =>
    fin - inicio > MaxDuracion;
```

**Por qué:** los tres puntos deben aplicar exactamente la misma comparación; duplicarla invita a que se desincronicen. Es el mismo patrón que ya usa `Overlaps`.

**Alternativa descartada:** validar en cada servicio con su propia resta. Se descarta por duplicación y porque un cambio futuro del límite requeriría tocar tres lugares.

### 2. Semántica del límite: `>` y no `>=`

Una duración de exactamente cuatro horas se acepta; solo se rechaza cuando la diferencia es estrictamente mayor. `TimeOnly` tiene precisión de segundos, por lo que `08:00:00–12:00:01` se rechaza y `08:00:00–12:00:00` se acepta.

**Por qué:** el requisito institucional es "máximo cuatro horas"; cuatro horas es el límite permitido, no el primer valor inválido.

### 3. Creación: validación estructural antes de la transacción

En `ReservationService.ValidateCreate` la comprobación de duración se inserta inmediatamente después de `HoraFin <= HoraInicio` y antes de la comprobación de fecha pasada. Se mantiene fuera de la transacción, junto con el resto de validaciones de entrada, para no abrir una transacción ni bloquear el espacio por una solicitud inválida.

**Por qué:** el orden actual ya valida lo estructural antes de tocar la base; la duración es una propiedad del intervalo, no del estado del sistema.

**Alternativa descartada:** validar dentro de la transacción. No aporta nada: la duración no depende de otros datos.

### 4. Aprobación: revalidación dentro de la transacción

En `ApproveAsync`, tras `CanTransition` y el bloqueo del espacio, se comprueba la duración antes de la comprobación de superposición. Si se excede, se lanza `409 Conflict` con un mensaje que indica el máximo y la reserva permanece `Pendiente` (no se muta nada antes del `throw`).

**Por qué:** el bloqueo ya está tomado y la operación es la última barrera para datos heredados. El `409` es coherente con los demás rechazos de aprobación (solicitud ya decidida, superposición), porque la petición en sí es válida y el conflicto está en el estado del recurso.

**Alternativa descartada:** `400 Bad Request`. Se reserva para entradas inválidas del cliente; aquí el cliente no envía cuerpo y el problema es la reserva almacenada.

### 5. Disponibilidad: misma validación en `Validate`

`AvailabilityService.Validate` incorpora la misma comprobación después de `HoraFin <= HoraInicio` y antes de la fecha pasada. Como ambos endpoints (`/api/availability/spaces` y `/api/availability/spaces/{id}`) comparten `Validate`, la regla cubre los dos sin código adicional. La respuesta es `400` con `detail` "La franja consultada no puede superar las cuatro horas consecutivas."

**Por qué:** el requisito pide que la consulta use la misma regla; centralizarla evita una tercera copia.

### 6. Sin restricción en la base de datos

La regla queda en la capa de aplicación, sin `CHECK` ni migración.

**Por qué:** un `CHECK` con `TIMEDIFF` obligaría a una migración, fallaría si existieran filas heredadas que lo incumplen y no aporta protección adicional frente a la API. La validación de aprobación cubre el caso heredado.

### 7. Pruebas: reemplazo de `DurationRuleTests`

El archivo actual afirma que una reserva de seis horas es aceptada; se reescribe para reflejar la nueva regla. Casos previstos:

- Creación: exactamente 4 h (`08:00–12:00`) → `201`; menos de 4 h (`08:00–11:00`) → `201`; 4 h + 1 min (`08:00–12:01`) → `400`; 6 h (`08:00–14:00`) → `400`, con el mensaje del límite.
- Disponibilidad: lista y detalle con exactamente 4 h → `200`; con más de 4 h → `400`.
- Aprobación: se inserta directamente en la base una reserva `Pendiente` de seis horas (simulando datos heredados, usando `Factory.Services.CreateScope()` y el `AcademicSpaceDbContext`), y al aprobarla por API se espera `409` y estado `Pendiente` tras la consulta.

Las pruebas de superposición, adyacencia y concurrencia existentes no cambian y deben seguir pasando.

## Risks / Trade-offs

- **Reservas heredadas de más de cuatro horas** → quedan en estado `Pendiente` y no se pueden aprobar; el aprobador debe rechazarlas con un motivo o el solicitante cancelarlas. Se documenta en `docs/api.md` y `docs/verificacion.md`.
- **Desincronización entre los tres puntos** → mitigado por el helper único y por pruebas en creación, aprobación y disponibilidad.
- **Mensaje distinto según el punto de entrada** (reserva vs. consulta) → intencional: cada mensaje describe la operación que el usuario intentó; ambos indican el máximo de cuatro horas.
- **Horario de pared, no duración absoluta** → `TimeOnly` no cruza medianoche y no hay zona horaria; cuatro horas significa cuatro horas de reloj dentro del mismo día. Es consistente con el resto del sistema y con el rango válido `HoraFin > HoraInicio`.

## Migration Plan

1. Desplegar el backend con la nueva validación. No hay migración ni cambio de datos.
2. Verificar con las pruebas automatizadas y un recorrido manual corto (crear 4 h, crear 5 h, consultar 4 h y 5 h, aprobar una reserva heredada).
3. Rollback: revertir el despliegue; los datos no se modifican, por lo que no requiere compensación.

## Open Questions

Ninguna. El límite fijo de cuatro horas y los códigos de respuesta (`400` en creación y disponibilidad, `409` en aprobación) quedan resueltos en este diseño.
