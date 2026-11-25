# Tasks

## 1. Estructura y configuración inicial

- [x] 1.1 Crear la solución `AcademicSpace.sln` con los proyectos `AcademicSpace.Api` (.NET 8 Web API) y `AcademicSpace.Tests` (xUnit), y verificar que `dotnet build` finaliza sin errores.
- [x] 1.2 Agregar los paquetes de EF Core, Pomelo MySQL, autenticación JWT y utilidades de prueba, y verificar que `dotnet restore` y `dotnet build` finalizan correctamente.
- [x] 1.3 Configurar `AcademicSpaceDbContext`, la cadena de conexión por variable de entorno y la estructura de módulos y capas descrita en `design.md`, y verificar que la API inicia con una base MySQL accesible.
- [x] 1.4 Configurar `ProblemDetails`, CORS para el origen del frontend, validación de modelos y manejo global de errores, y verificar que una petición inválida devuelve `400` con el formato de error definido.
- [x] 1.5 Documentar en `README.md` los requisitos, variables de entorno, comandos de ejecución y estructura del proyecto, y verificar que los comandos documentados funcionan en una copia limpia.

## 2. Modelo de datos y persistencia

- [x] 2.1 Implementar las entidades `Rol`, `Usuario`, `Espacio`, `Reserva` y `Sesion` con sus claves, relaciones, índices únicos y de consulta, y verificar que la migración inicial crea las tablas e índices en MySQL.
- [x] 2.2 Configurar la siembra de los roles `Administrador`, `Solicitante` y `Aprobador` y del administrador inicial desde variables de entorno, y verificar mediante una prueba que los roles son únicos y que existe una cuenta administradora inicial con el correo configurado.
- [x] 2.3 Configurar el proyecto de pruebas con una base MySQL de pruebas y `WebApplicationFactory`, aplicando migraciones antes de cada ejecución, y verificar que una prueba de humo de integración pasa.
- [x] 2.4 Documentar el modelo de datos y las reglas de conservación (sin borrado físico) en `docs/modelo-datos.md`, y verificar que el documento coincide con las entidades y migraciones implementadas.

## 3. Autenticación y control de acceso

- [x] 3.1 Implementar el inicio de sesión con verificación de contraseña, emisión de JWT, registro de `Sesion` y rechazo de usuarios inactivos, y verificar con pruebas los casos válido, contraseña incorrecta y cuenta inactiva.
- [x] 3.2 Implementar el cierre de sesión revocando la sesión y la validación de sesión activa en cada petición autenticada, y verificar que una petición posterior al logout recibe `401`.
- [x] 3.3 Configurar las políticas de autorización para los tres roles y verificar con pruebas que un endpoint protegido responde `401` sin sesión y `403` cuando el rol no corresponde; cada grupo de endpoints aplicará su política al implementarse.
- [x] 3.4 Implementar la revocación de todas las sesiones de un usuario al desactivarlo, y verificar que sus tokens dejan de ser aceptados inmediatamente.
- [x] 3.5 Documentar en `README.md` el flujo de autenticación y las variables del token, y verificar que el flujo descrito funciona contra la API local.

## 4. Gestión de usuarios

- [x] 4.1 Implementar el registro de usuarios por el administrador con validación de nombre, correo único, contraseña mínima y rol existente, y verificar con pruebas el registro válido, el correo duplicado y los datos inválidos.
- [x] 4.2 Implementar la consulta de todos los usuarios y del detalle por el administrador, y la consulta del perfil propio, sin devolver contraseñas, y verificar con pruebas los casos permitidos y el `403` al consultar otro perfil.
- [x] 4.3 Implementar la actualización de usuario por el administrador (nombre, correo, rol y estado) y la actualización del perfil propio (solo nombre y correo), y verificar con pruebas la unicidad del correo y el rechazo de cambios de rol o estado propios.
- [x] 4.4 Implementar la desactivación y reactivación de usuarios, y verificar con pruebas que un usuario inactivo no puede iniciar sesión y que al reactivarlo puede hacerlo de nuevo.
- [x] 4.5 Documentar los endpoints de usuarios y sus permisos en `docs/api.md`, y verificar que cada endpoint documentado responde según el rol indicado.

## 5. Gestión de espacios académicos

- [x] 5.1 Implementar el registro de espacios con nombre único, tipo permitido, ubicación y capacidad mayor que cero, y verificar con pruebas el registro válido, el tipo inválido, el nombre duplicado y la capacidad inválida.
- [x] 5.2 Implementar la consulta de espacios activos para usuarios autenticados y la consulta completa para el administrador, incluyendo detalle y estado, y verificar con pruebas ambos alcances.
- [x] 5.3 Implementar la modificación de nombre, tipo, ubicación, capacidad y estado por el administrador, y verificar con pruebas la actualización válida y el rechazo de nombres duplicados.
- [x] 5.4 Implementar la desactivación como única forma de baja, sin endpoint de eliminación física, y verificar con pruebas que el espacio inactivo deja de ofrecerse en la consulta de espacios para usuarios no administradores y que dicho endpoint de eliminación no existe.
- [x] 5.5 Documentar los endpoints de espacios en `docs/api.md`, y verificar que las restricciones de tipo y capacidad descritas coinciden con las pruebas.

## 6. Consulta de disponibilidad

- [x] 6.1 Implementar el cálculo de franjas ocupadas por reservas pendientes y aprobadas para un espacio, fecha y franja, sin exponer datos del solicitante, y verificar con pruebas de superposición parcial, total y sin conflicto.
- [x] 6.2 Implementar la consulta de espacios activos disponibles para una fecha y franja, con filtro opcional por tipo, y verificar con pruebas la lista completa, el filtro por laboratorio y la lista vacía.
- [x] 6.3 Implementar la validación de fecha futura y de hora de fin posterior a la hora de inicio, y verificar con pruebas que las consultas inválidas reciben `400`.
- [x] 6.4 Verificar con pruebas que las reservas rechazadas y canceladas no ocupan franjas y que los espacios inactivos nunca aparecen como disponibles.
- [x] 6.5 Documentar la semántica de disponibilidad y los endpoints en `docs/api.md`, y verificar que los ejemplos coinciden con las respuestas de las pruebas.

## 7. Solicitudes de reserva y prevención de conflictos

- [x] 7.1 Implementar la creación de solicitudes por el rol solicitante con espacio activo, fecha futura, horas válidas y motivo, asignando estado pendiente y propietario, y verificar con pruebas los casos válidos e inválidos.
- [x] 7.2 Implementar el cálculo de superposición con intervalos semiabiertos (`inicio < finAjeno` y `fin > inicioAjeno`), y verificar con pruebas unitarias superposición parcial, superposición total, intervalos adyacentes y espacios distintos.
- [x] 7.3 Implementar la creación transaccional con bloqueo de la fila del espacio, comprobación de conflictos e inserción, y verificar con una prueba de concurrencia que dos solicitudes simultáneas superpuestas producen una sola reserva registrada.
- [x] 7.4 Implementar la consulta de solicitudes propias para el solicitante y de todas las solicitudes para aprobador y administrador, con detalle protegido por permisos, y verificar con pruebas los alcances y el `403` correspondiente.
- [x] 7.5 Implementar el ciclo de vida con los estados pendiente, aprobada, rechazada y cancelada y el rechazo de transiciones no permitidas, y verificar con pruebas cada transición válida y las finales.
- [x] 7.6 Verificar con pruebas de integración que una solicitud para un espacio inactivo o en una franja ocupada se rechaza sin crear el registro y con el código de error definido.
- [x] 7.7 Verificar con una prueba de integración que las reservas existentes de un espacio desactivado conservan su estado y siguen siendo consultables.
- [x] 7.8 Documentar en `docs/api.md` la creación, consulta y estados de las solicitudes, y verificar que los ejemplos de error de conflicto coinciden con las pruebas.

## 8. Aprobaciones y rechazos

- [x] 8.1 Implementar la consulta de solicitudes pendientes para el responsable de aprobación, ordenadas de la más antigua a la más reciente, y verificar con pruebas la lista con datos y la lista vacía.
- [x] 8.2 Implementar la aprobación transaccional con revalidación de conflictos, cambio a aprobada y registro de aprobador y fecha, y verificar con pruebas la aprobación exitosa, el conflicto y el `403` de otros roles.
- [x] 8.3 Implementar el rechazo con motivo obligatorio, cambio a rechazada y registro de aprobador, fecha y motivo, y verificar con pruebas el rechazo exitoso, la falta de motivo y el `409` de una solicitud ya decidida.
- [x] 8.4 Verificar con pruebas de integración que una solicitud aprobada deja de estar pendiente y que su detalle e historial muestran la trazabilidad de la decisión.
- [x] 8.5 Documentar los endpoints de aprobación y rechazo en `docs/api.md`, y verificar que los permisos documentados coinciden con las pruebas.

## 9. Cancelaciones e historial

- [x] 9.1 Implementar la cancelación de solicitudes propias en estado pendiente o aprobada con registro de fecha, y verificar con pruebas los casos válidos y el rechazo de solicitudes ajenas o ya finalizadas.
- [x] 9.2 Verificar con una prueba de integración que al cancelar una reserva aprobada la franja vuelve a aparecer disponible y puede aceptar una nueva solicitud.
- [x] 9.3 Implementar el historial del solicitante con todas sus solicitudes y estados, y verificar con pruebas el orden cronológico, los registros con decisión o cancelación y la lista vacía.
- [x] 9.4 Implementar el historial del responsable de aprobación (incluidas las reservas aprobadas) y el historial completo del administrador, y verificar con pruebas los alcances por rol.
- [x] 9.5 Verificar con pruebas que las reservas rechazadas o canceladas permanecen consultables y no se eliminan del historial.
- [x] 9.6 Documentar el historial y las reglas de cancelación en `docs/api.md`, y verificar que los ejemplos de estados finales coinciden con las pruebas.

## 10. Interfaz web en React

- [x] 10.1 Crear la aplicación React con TypeScript y Vite, el enrutador, el cliente HTTP y el contexto de autenticación, y verificar que `npm run build` finaliza sin errores.
- [x] 10.2 Implementar las pantallas de inicio y cierre de sesión con manejo de errores y almacenamiento del token en `sessionStorage`, y verificar manualmente el flujo con un administrador sembrado.
- [x] 10.3 Implementar las pantallas de administración de usuarios (listado, registro, edición y estado), y verificar manualmente que un administrador completa el flujo y un solicitante no accede a la ruta.
- [x] 10.4 Implementar las pantallas de administración de espacios (listado, registro, edición y desactivación), y verificar manualmente el flujo completo con un administrador.
- [x] 10.5 Implementar la consulta de disponibilidad por fecha, franja y tipo con visualización de franjas ocupadas, y verificar manualmente que una reserva existente bloquea la franja.
- [x] 10.6 Implementar el formulario de solicitud de reserva y la lista de solicitudes propias con su estado, y verificar manualmente la creación y el rechazo por conflicto.
- [x] 10.7 Implementar la bandeja de pendientes y las acciones de aprobar y rechazar con motivo, y verificar manualmente el flujo completo con el responsable de aprobación.
- [x] 10.8 Implementar la cancelación de reservas propias y las vistas de historial por rol, y verificar manualmente que la reserva cancelada aparece en el historial y libera la franja.
- [x] 10.9 Implementar la protección de rutas por rol y el manejo global de `401`/`403`, y verificar manualmente que cada rol solo ve sus opciones y que una sesión expirada redirige al login.
- [x] 10.10 Documentar en `docs/frontend.md` la estructura, las rutas y los roles de cada pantalla, y verificar que la documentación coincide con la aplicación construida.

## 11. Verificación de integración

- [x] 11.1 Ejecutar `dotnet test` y `npm run build` y verificar que todas las pruebas y la compilación terminan sin errores.
- [x] 11.2 Ejecutar un recorrido manual de extremo a extremo con los tres roles (registro, espacio, disponibilidad, solicitud, aprobación, cancelación e historial) y registrar el resultado en `docs/verificacion.md`.
- [x] 11.3 Verificar que el sistema no implementa la restricción de cuatro horas y que solo aplica la regla de no superposición, mediante búsqueda en el código y una prueba de una reserva de más de cuatro horas permitida.
- [x] 11.4 Verificar que `openspec validate initial-system` no reporta errores y que los artefactos del cambio coinciden con lo implementado; los avisos sobre palabras normativas en inglés son esperados porque las especificaciones están redactadas en español.
