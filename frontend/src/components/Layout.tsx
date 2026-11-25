import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import type { Rol } from '../api/types';

interface NavItem {
  to: string;
  label: string;
}

const NAV: Record<Rol, NavItem[]> = {
  Administrador: [
    { to: '/', label: 'Inicio' },
    { to: '/usuarios', label: 'Usuarios' },
    { to: '/espacios', label: 'Espacios' },
    { to: '/disponibilidad', label: 'Disponibilidad' },
    { to: '/reservas', label: 'Reservas' },
    { to: '/historial', label: 'Historial' }
  ],
  Solicitante: [
    { to: '/', label: 'Inicio' },
    { to: '/disponibilidad', label: 'Disponibilidad' },
    { to: '/mis-solicitudes', label: 'Mis solicitudes' },
    { to: '/historial', label: 'Historial' }
  ],
  Aprobador: [
    { to: '/', label: 'Inicio' },
    { to: '/pendientes', label: 'Pendientes' },
    { to: '/reservas', label: 'Reservas' },
    { to: '/historial', label: 'Historial' }
  ]
};

export function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  if (!user) {
    return null;
  }

  async function handleLogout() {
    await logout();
    navigate('/login', { replace: true });
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">AS</span>
          <div>
            <strong>AcademicSpace</strong>
            <small>Gestión de reservas</small>
          </div>
        </div>
        <nav className="nav">
          {NAV[user.rol].map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="main">
        <header className="topbar">
          <div>
            <span className="topbar-label">Sesión activa</span>
            <strong>{user.nombre}</strong>
          </div>
          <div className="topbar-actions">
            <span className="role-chip">{user.rol}</span>
            <button type="button" className="button ghost" onClick={handleLogout}>
              Cerrar sesión
            </button>
          </div>
        </header>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
