import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import type { Rol } from '../api/types';

const ACCIONES: Record<Rol, { to: string; titulo: string; detalle: string }[]> = {
  Administrador: [
    { to: '/usuarios', titulo: 'Gestionar usuarios', detalle: 'Registre usuarios, asigne roles y controle su estado.' },
    { to: '/espacios', titulo: 'Gestionar espacios', detalle: 'Registre laboratorios, auditorios y salas de cómputo.' },
    { to: '/disponibilidad', titulo: 'Consultar disponibilidad', detalle: 'Verifique franjas libres antes de reservar.' },
    { to: '/reservas', titulo: 'Revisar reservas', detalle: 'Consulte todas las solicitudes del sistema.' }
  ],
  Solicitante: [
    { to: '/disponibilidad', titulo: 'Consultar disponibilidad', detalle: 'Busque espacios libres por fecha, hora y tipo.' },
    { to: '/mis-solicitudes', titulo: 'Mis solicitudes', detalle: 'Cree reservas y cancele las que ya no necesite.' },
    { to: '/historial', titulo: 'Mi historial', detalle: 'Revise el estado de todas sus solicitudes.' }
  ],
  Aprobador: [
    { to: '/pendientes', titulo: 'Solicitudes pendientes', detalle: 'Apruebe o rechace las solicitudes en espera.' },
    { to: '/reservas', titulo: 'Todas las reservas', detalle: 'Consulte el detalle de las reservas registradas.' },
    { to: '/historial', titulo: 'Historial', detalle: 'Revise decisiones y cancelaciones anteriores.' }
  ]
};

export function HomePage() {
  const { user } = useAuth();
  if (!user) {
    return null;
  }

  return (
    <div className="stack">
      <section className="hero-panel">
        <p className="eyebrow">Panel principal</p>
        <h1>Bienvenido, {user.nombre}</h1>
        <p>
          Su rol es <strong>{user.rol}</strong>. Desde este panel puede acceder a las funciones
          habilitadas para su perfil.
        </p>
      </section>

      <section className="card-grid">
        {ACCIONES[user.rol].map((accion) => (
          <Link key={accion.to} to={accion.to} className="action-card">
            <strong>{accion.titulo}</strong>
            <span>{accion.detalle}</span>
          </Link>
        ))}
      </section>
    </div>
  );
}
