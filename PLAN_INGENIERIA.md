# PLAN DE INGENIERÍA DE SOFTWARE
## AcademicSpace – Sistema de Gestión de Reservas Académicas

## 1. Análisis del problema

### 1.1 Identificación de la problemática

Actualmente, la institución universitaria administra las reservas de espacios académicos mediante correos electrónicos y hojas de cálculo. Este procedimiento dificulta la organización de las solicitudes y puede generar problemas como:

- Duplicidad de solicitudes para un mismo espacio.
- Conflictos entre horarios de reservas.
- Dificultad para consultar la disponibilidad de los espacios.
- Falta de seguimiento sobre las solicitudes realizadas.
- Demoras en los procesos de aprobación.

Por esta razón, se requiere desarrollar un sistema centralizado que permita administrar los espacios académicos y gestionar sus reservas de manera organizada.

### 1.2 Necesidades de la institución

El sistema deberá permitir:

- Registrar y administrar espacios académicos.
- Registrar usuarios y asignarles roles.
- Consultar la disponibilidad de los espacios.
- Crear solicitudes de reserva.
- Validar que no existan conflictos de horarios.
- Aprobar o rechazar solicitudes.
- Cancelar reservas.
- Consultar el historial de solicitudes y reservas.

### 1.3 Actores involucrados

Se identifican los siguientes actores:

**Administrador:**
- Registrar, modificar y desactivar espacios académicos.
- Registrar y administrar usuarios.
- Consultar las reservas del sistema.

**Usuario solicitante (estudiante o docente):**
- Consultar los espacios disponibles.
- Crear solicitudes de reserva.
- Consultar el estado de sus solicitudes.
- Cancelar sus reservas.

**Responsable de aprobación:**
- Consultar solicitudes pendientes.
- Aprobar o rechazar solicitudes.
- Consultar las reservas aprobadas.

### 1.4 Alcance del sistema

El proyecto contempla el desarrollo de una aplicación web para centralizar la gestión de reservas de laboratorios, auditorios y salas de cómputo.

El sistema incluirá autenticación, administración de usuarios, gestión de espacios, solicitudes de reserva, validación de disponibilidad, aprobación, cancelación e historial.

**Limitaciones:**

- No se implementarán pagos.
- No se integrará con sistemas académicos externos.
- No se desarrollará una aplicación móvil.
- No se implementarán notificaciones mediante SMS.
- La administración se realizará desde una aplicación web.

---

## 2. Propuesta de solución

### 2.1 Descripción general

Se propone desarrollar AcademicSpace, una aplicación web que centralice la administración de los espacios académicos y sus respectivas reservas.

La solución contará con una interfaz web desde la cual los usuarios podrán consultar los espacios, solicitar reservas y realizar seguimiento de sus solicitudes.

El backend será responsable de aplicar las reglas del negocio, validar la disponibilidad y administrar las operaciones sobre la base de datos.

### 2.2 Funcionalidades prioritarias

Las funcionalidades se desarrollarán en el siguiente orden:

1. Registro y autenticación de usuarios.
2. Administración de usuarios y roles.
3. Gestión de espacios académicos.
4. Consulta de disponibilidad.
5. Creación de solicitudes de reserva.
6. Validación de conflictos de horarios.
7. Aprobación y rechazo de solicitudes.
8. Cancelación e historial de reservas.

La restricción de máximo cuatro horas consecutivas se incorporará posteriormente como un cambio de requisitos solicitado por la institución.

### 2.3 Organización del sistema en módulos

El sistema estará organizado en los siguientes módulos:

**Módulo de usuarios:**
Gestionará el registro, autenticación y administración de usuarios y roles.

**Módulo de espacios:**
Permitirá registrar, consultar, modificar y desactivar espacios académicos.

**Módulo de reservas:**
Gestionará las solicitudes, validaciones de disponibilidad, aprobaciones, rechazos y cancelaciones.

**Módulo de consultas:**
Permitirá consultar la disponibilidad y el historial de reservas.

### 2.4 Estrategia de desarrollo progresivo

El proyecto se desarrollará por funcionalidades, comenzando por los componentes fundamentales y continuando con los procesos que dependen de ellos.

Cada funcionalidad deberá contar primero con una especificación revisada en OpenSpec, posteriormente se realizará su implementación y finalmente se ejecutarán las pruebas correspondientes.

---

## 3. Decisiones de arquitectura y tecnología

### 3.1 Arquitectura seleccionada

Se utilizará una arquitectura de monolito modular organizada por capas.

La solución estará dividida en tres componentes principales:

- **Presentación:** aplicación web desarrollada en React.
- **Lógica de negocio:** API REST desarrollada en .NET 8.
- **Persistencia:** base de datos MySQL administrada mediante Entity Framework Core.

Esta arquitectura permite mantener separadas las responsabilidades del sistema sin introducir complejidad innecesaria.

### 3.2 Stack tecnológico

| Tecnología | Uso |
|---|---|
| C# | Lenguaje del backend |
| .NET 8 Web API | Desarrollo de servicios REST |
| React | Desarrollo de interfaces |
| TypeScript | Tipado del frontend |
| MySQL | Almacenamiento de información |
| Entity Framework Core | Acceso a datos |
| xUnit | Pruebas automatizadas |
| Git | Control de versiones |
| GitHub | Almacenamiento del repositorio |
| OpenSpec | Gestión de especificaciones |

### 3.3 Estrategia de persistencia

Se utilizará MySQL como sistema de gestión de bases de datos relacional.

Se contemplan las siguientes entidades principales:

- Usuario.
- Rol.
- Espacio.
- Reserva.

Las relaciones entre estas entidades permitirán asociar cada solicitud con el usuario responsable y el espacio académico solicitado.

Las operaciones de reserva utilizarán transacciones y validaciones en el backend para evitar conflictos cuando varios usuarios intenten reservar un mismo espacio.

Se considerarán conflictivas las reservas pendientes y aprobadas que coincidan en el mismo espacio y tengan horarios superpuestos.

### 3.4 Organización de componentes

La estructura general del proyecto será:

- **Frontend:** componentes, páginas, servicios y modelos.
- **Backend:** controladores, servicios, entidades y repositorios.
- **Base de datos:** tablas y relaciones correspondientes a las entidades del sistema.
- **Pruebas:** proyectos de pruebas automatizadas para verificar las reglas del negocio.

### 3.5 Comparación de decisiones técnicas

**Decisión 1: Backend**

Alternativa seleccionada: .NET 8 Web API.

Alternativas descartadas:
- Spring Boot.
- Node.js con Express.

Justificación: .NET 8 permite desarrollar una API REST organizada, cuenta con herramientas para pruebas automatizadas y es una tecnología con la que se tiene experiencia previa.

**Decisión 2: Base de datos**

Alternativa seleccionada: MySQL.

Alternativas descartadas:
- MongoDB.
- PostgreSQL.

Justificación: MySQL permite trabajar con información estructurada y relaciones entre usuarios, espacios y reservas. Además, facilita el manejo de transacciones para proteger la consistencia de los datos.

**Decisión 3: Frontend**

Alternativa seleccionada: React con TypeScript.

Alternativas descartadas:
- Angular.
- Vue.js.

Justificación: React permite construir interfaces mediante componentes reutilizables y TypeScript facilita la detección de errores relacionados con los tipos de datos.

**Decisión 4: Arquitectura**

Alternativa seleccionada: monolito modular por capas.

Alternativa descartada: arquitectura de microservicios.

Justificación: el sistema tiene un alcance limitado y no requiere desplegar servicios independientes. Una arquitectura modular permite mantener una organización clara con menor complejidad de desarrollo y mantenimiento.

---

## 4. Estrategia de desarrollo

### 4.1 Metodología de trabajo

Se utilizará un enfoque incremental apoyado en Spec-Driven Development (SDD).

El desarrollo estará guiado por especificaciones que describirán el comportamiento esperado de cada funcionalidad.

Se utilizará OpenSpec para documentar los requisitos, escenarios de aceptación y cambios solicitados.

Git y GitHub permitirán mantener el historial de modificaciones y evidenciar el proceso de desarrollo.

### 4.2 Etapas de implementación

**Etapa 1. Planificación:**
Análisis del problema, definición del alcance y selección de tecnologías.

**Etapa 2. Especificación:**
Inicialización de OpenSpec y documentación de los requisitos funcionales y restricciones.

**Etapa 3. Revisión:**
Verificación de las especificaciones, identificación de ambigüedades y definición de tareas.

**Etapa 4. Implementación:**
Desarrollo progresivo del backend, frontend y persistencia.

**Etapa 5. Gestión de cambios:**
Incorporación de la nueva regla que limita las reservas a cuatro horas consecutivas.

**Etapa 6. Verificación:**
Ejecución de pruebas, revisión de requisitos y documentación de resultados.

### 4.3 Orden y dependencias

El orden de desarrollo será:

1. Base de datos y configuración del backend.
2. Usuarios y roles.
3. Gestión de espacios académicos.
4. Gestión de reservas.
5. Validación de disponibilidad y conflictos.
6. Aprobación, rechazo y cancelación.
7. Historial y consultas.
8. Interfaz de usuario.
9. Nueva regla de máximo cuatro horas.

Las reservas dependen de la existencia de usuarios y espacios. Por esta razón, estos módulos deberán desarrollarse primero.

### 4.4 Criterios de finalización

Una funcionalidad se considerará terminada cuando:

- Su especificación esté documentada y revisada.
- Su implementación cumpla los requisitos establecidos.
- Las pruebas correspondientes sean satisfactorias.
- No existan errores conocidos que impidan su funcionamiento.
- Los cambios estén registrados en Git.

---

## 5. Estrategia de calidad

### 5.1 Tipos de pruebas

Se realizarán los siguientes tipos de pruebas:

**Pruebas unitarias:**
Verificarán las reglas de negocio, como la validación de horarios y la duración de las reservas.

**Pruebas de integración:**
Comprobarán la comunicación entre los servicios del backend y la base de datos.

**Pruebas funcionales:**
Verificarán que las funcionalidades cumplan los requisitos establecidos.

**Pruebas de validación:**
Comprobarán que el sistema rechace información incorrecta y solicitudes que incumplan las restricciones.

### 5.2 Identificación de riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Reservas simultáneas para un mismo espacio | Validaciones en backend y operaciones transaccionales. |
| Errores en los horarios de reserva | Validar fechas, horas y superposición de intervalos. |
| Acceso no autorizado | Implementar autenticación y control de permisos por roles. |
| Pérdida o inconsistencia de información | Utilizar transacciones y restricciones en la base de datos. |
| Errores en el código generado por IA | Revisar el código y ejecutar pruebas automatizadas. |
| Cambios en los requisitos | Actualizar primero las especificaciones mediante OpenSpec. |

### 5.3 Verificación de requisitos

El cumplimiento de los requisitos se verificará mediante:

- Escenarios de aceptación documentados en OpenSpec.
- Pruebas automatizadas asociadas a las funcionalidades.
- Revisión manual del funcionamiento del sistema.
- Comparación entre las especificaciones y la implementación.
- Historial de Git para comprobar la trazabilidad de los cambios.

---

## 6. Uso de SDD e inteligencia artificial

### 6.1 Utilización de OpenSpec

OpenSpec será utilizado como herramienta para administrar las especificaciones del sistema y organizar el desarrollo mediante cambios documentados.

Cada funcionalidad contará con requisitos y escenarios de aceptación que permitan determinar su comportamiento esperado.

### 6.2 Transformación del plan de ingeniería en especificaciones

El documento PLAN_INGENIERIA.md servirá como punto de partida para construir las especificaciones.

A partir de sus funcionalidades y restricciones se definirán los requisitos correspondientes a cada módulo.

Posteriormente, se revisarán las especificaciones para garantizar que sean claras, consistentes y verificables antes de comenzar la implementación.

### 6.3 Actividades delegadas a la inteligencia artificial

Se utilizará un agente de programación como apoyo para:

- Proponer estructuras de código.
- Generar componentes del frontend.
- Desarrollar servicios y controladores del backend.
- Elaborar pruebas automatizadas.
- Identificar errores y proponer correcciones.
- Apoyar la documentación técnica.

### 6.4 Verificación del código generado

El código generado por inteligencia artificial será revisado antes de integrarse al proyecto.

Se verificará que:

- Cumpla las especificaciones establecidas.
- Respete la arquitectura definida.
- Implemente correctamente las reglas del negocio.
- No introduzca errores en otras funcionalidades.
- Supere las pruebas automatizadas correspondientes.

### 6.5 Responsabilidades del estudiante

El estudiante será responsable de:

- Analizar el problema y definir los requisitos.
- Seleccionar y justificar las decisiones técnicas.
- Revisar y aprobar las especificaciones.
- Supervisar el código generado por inteligencia artificial.
- Ejecutar y analizar las pruebas.
- Gestionar los cambios mediante OpenSpec.
- Mantener la trazabilidad utilizando Git.
- Verificar que el sistema cumpla los objetivos establecidos.

La inteligencia artificial será una herramienta de apoyo y no reemplazará las decisiones ni la responsabilidad técnica del estudiante.

---

## 7. Conclusión

La planificación de AcademicSpace establece una solución tecnológica para mejorar la administración de los espacios académicos de la institución.

La arquitectura propuesta y las tecnologías seleccionadas permiten desarrollar el sistema de manera progresiva, manteniendo una separación clara de responsabilidades.

El uso de Spec-Driven Development y OpenSpec permitirá que las especificaciones sean la base de la implementación y que los cambios realizados mantengan una trazabilidad verificable.

Finalmente, mediante Git, las pruebas automatizadas y la revisión del código generado por inteligencia artificial se buscará garantizar la calidad y el cumplimiento de los requisitos del proyecto.