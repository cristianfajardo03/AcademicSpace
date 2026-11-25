import { useEffect, useState } from 'react';
import { ApiError, api } from '../api/client';
import type { Reservation } from '../api/types';
import { EstadoBadge } from '../components/EstadoBadge';
import { formatDate, formatDateTime, formatTime } from '../utils/format';

export function ReservationsPage() {
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const data = await api.listReservations();
        if (active) {
          setReservations(data);
          setError(null);
        }
      } catch (caught) {
        if (active) {
          setError(caught instanceof ApiError ? caught.message : 'No se pudieron cargar las reservas.');
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      active = false;
    };
  }, []);

  return (
    <div className="stack">
      <header className="page-header">
        <div>
          <h1>Reservas registradas</h1>
          <p>Consulte el estado y la trazabilidad de todas las solicitudes del sistema.</p>
        </div>
      </header>

      {error && <p className="alert error">{error}</p>}

      <section className="card">
        {loading ? (
          <p className="muted">Cargando reservas…</p>
        ) : (
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Espacio</th>
                  <th>Solicitante</th>
                  <th>Fecha</th>
                  <th>Franja</th>
                  <th>Estado</th>
                  <th>Aprobador</th>
                  <th>Decisión</th>
                </tr>
              </thead>
              <tbody>
                {reservations.map((reservation) => (
                  <tr key={reservation.id}>
                    <td>{reservation.espacioNombre}</td>
                    <td>{reservation.solicitanteNombre}</td>
                    <td>{formatDate(reservation.fecha)}</td>
                    <td>
                      {formatTime(reservation.horaInicio)} – {formatTime(reservation.horaFin)}
                    </td>
                    <td>
                      <EstadoBadge estado={reservation.estado} />
                    </td>
                    <td>{reservation.aprobadorNombre ?? '—'}</td>
                    <td>{formatDateTime(reservation.fechaDecision)}</td>
                  </tr>
                ))}
                {reservations.length === 0 && (
                  <tr>
                    <td colSpan={7} className="muted">
                      No hay reservas registradas.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
