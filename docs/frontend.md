# Frontend

Aplicación React con TypeScript, creada con Vite. Consume la API de AcademicSpace mediante
`fetch`.

## Comandos

```bash
cd frontend
npm install
npm run dev      # servidor de desarrollo en http://localhost:5173
npm run build    # verificación de tipos y compilación de producción
npm run preview  # sirve la compilación de dist/
```

Variable de entorno opcional:

| Variable | Valor por defecto | Descripción |
| --- | --- | --- |
| `VITE_API_URL` | `http://localhost:5199` | URL base de la API. |

La API debe permitir el origen del frontend en CORS (`Cors__AllowedOrigins__0`, por defecto
`http://localhost:5173`).

## Estructura

```text
frontend/src/
├── api/
│   ├── client.ts       # Cliente HTTP, manejo de errores y sesión en sessionStorage
│   └── types.ts        # Tipos del contrato y catálogos de espacios
├── auth/
│   └── AuthContext.tsx # Estado de autenticación, login, logout y validación inicial
├── components/
│   ├── EstadoBadge.tsx # Etiqueta visual del estado de una reserva
│   ├── Layout.tsx      # Barra lateral, encabezado y navegación por rol
│   └── RequireAuth.tsx # Protección de rutas por sesión y rol
├── pages/
│   ├── LoginPage.tsx
│   ├── HomePage.tsx
│   ├── UsersPage.tsx
│   ├── SpacesPage.tsx
│   ├── AvailabilityPage.tsx
│   ├── MyReservationsPage.tsx
│   ├── ApprovalsPage.tsx
│   ├── ReservationsPage.tsx
│   └── HistoryPage.tsx
├── utils/format.ts     # Formatos de fecha y hora
├── App.tsx             # Definición de rutas
├── main.tsx            # Punto de entrada, router y proveedor de autenticación
└── index.css           # Estilos globales
```

## Rutas y roles

| Ruta | Pantalla | Roles permitidos |
| --- | --- | --- |
| `/login` | Inicio de sesión | Público (redirige al inicio si ya hay sesión) |
| `/` | Panel principal con accesos por rol | Todos los autenticados |
| `/usuarios` | Listado, registro, edición y estado de usuarios | `Administrador` |
| `/espacios` | Listado, registro, edición y desactivación de espacios | `Administrador` |
| `/disponibilidad` | Consulta por fecha, franja y tipo; detalle de franjas ocupadas | Todos los autenticados |
| `/mis-solicitudes` | Formulario de solicitud, listado propio y cancelación | `Solicitante` |
| `/pendientes` | Bandeja de solicitudes pendientes con aprobar/rechazar | `Aprobador` |
| `/reservas` | Todas las reservas con estado y trazabilidad | `Administrador`, `Aprobador` |
| `/historial` | Historial según el alcance del rol | Todos los autenticados |
| `*` | Redirección al inicio | — |

Si un usuario autenticado intenta abrir una ruta fuera de su rol, `RequireAuth` lo redirige a `/`.
Si no hay sesión, lo redirige a `/login`.

## Autenticación y sesión

- El token JWT y el usuario se guardan en `sessionStorage` (`academicspace.token` y
  `academicspace.user`).
- Al cargar la aplicación, si existe un token se consulta `GET /api/users/me` para validarlo y
  refrescar los datos del usuario.
- Cualquier respuesta `401` limpia la sesión, emite el evento `academicspace:unauthorized` y el
  contexto deja al usuario sin sesión; las rutas protegidas redirigen a `/login`.
- Cualquier respuesta `403` muestra el mensaje de permisos insuficientes devuelto por la API.
- El cierre de sesión llama a `POST /api/auth/logout`, limpia `sessionStorage` y vuelve a `/login`.

## Comportamiento por pantalla

- **Usuarios:** el formulario alterna entre registro y edición. En edición no se solicita
  contraseña y se puede cambiar el estado. El correo debe ser único.
- **Espacios:** el formulario alterna entre registro y edición. Los espacios se desactivan o
  reactivan; no hay acción de eliminación.
- **Disponibilidad:** consulta los espacios libres para una fecha y franja. El botón «Ver franjas»
  muestra los intervalos ocupados del espacio seleccionado, sin datos del solicitante.
- **Mis solicitudes:** crea reservas en estado pendiente. Los errores de conflicto (`409`) se
  muestran como alerta. Las reservas pendientes o aprobadas se pueden cancelar.
- **Pendientes:** muestra las solicitudes en espera; permite aprobar o rechazar con un motivo
  obligatorio.
- **Reservas e historial:** muestran el estado, el aprobador, la fecha de decisión, el motivo de
  rechazo o la fecha de cancelación según corresponda.
