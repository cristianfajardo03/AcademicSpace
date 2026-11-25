# Tasks

## 1. Regla de duración compartida

- [x] 1.1 Agregar a `ReservationRules.cs` la constante `MaxDuracion` de cuatro horas y el predicado `ExceedsMaxDuration(inicio, fin)`.
- [x] 1.2 Confirmar ejecución de `ReservationRulesTests` mediante el filtro específico.
- [x] 1.3 Actualizar `docs/modelo-datos.md` y `README.md` con la regla de máximo cuatro horas.

## 2. Validación al crear solicitudes

- [x] 2.1 Incorporar la comprobación de duración en `ReservationService.ValidateCreate`.
- [x] 2.2 Implementar pruebas para reservas de tres horas, cuatro horas, cuatro horas y un minuto, y seis horas. Verificar respuestas HTTP.
- [x] 2.3 Documentar en `docs/api.md` la validación de duración al crear solicitudes y el error `400`.

## 3. Validación al aprobar solicitudes

- [x] 3.1 Revalidar la duración en `ReservationService.ApproveAsync`, respondiendo `409` cuando se supera el límite.
- [x] 3.2 Implementar prueba de integración para una reserva heredada de seis horas, comprobando `409` y conservación del estado `Pendiente`.
- [x] 3.3 Documentar en `docs/api.md` la revalidación de duración y el tratamiento de reservas heredadas.

## 4. Validación en la consulta de disponibilidad

- [x] 4.1 Incorporar la validación de duración en `AvailabilityService.Validate`.
- [x] 4.2 Implementar pruebas para consultas de disponibilidad de cuatro horas y superiores a cuatro horas, tanto en lista como en detalle.
- [x] 4.3 Documentar en `docs/api.md` la validación de duración en disponibilidad y el error `400`.

## 5. Integración y verificación final

- [x] 5.1 Ejecutar `dotnet test` completo y `npm run build`, verificando que todas las pruebas pasan y que el frontend compila correctamente.
- [ ] 5.2 Ejecutar un recorrido manual contra la API local y comprobar creación, disponibilidad y aprobación de reservas.
- [x] 5.3 Actualizar `docs/verificacion.md` con la evidencia de la regla de cuatro horas y las pruebas específicas.
- [ ] 5.4 Ejecutar `openspec validate reservation-max-duration` y confirmar que no reporta errores.
