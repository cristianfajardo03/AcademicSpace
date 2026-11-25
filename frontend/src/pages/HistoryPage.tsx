import { useEffect, useState } from 'react';
import { ApiError, api } from '../api/client';
import type { Reservation } from '../api/types';
import { EstadoBadge } from '../components/EstadoBadge';
import { formatDate, formatDateTime, formatTime } from '../utils/format';

export function HistoryPage() {
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const data = await api.history();
        if (active) {
          setReservations(data);
          setError(null);
        }
      } catch (caught) {
        if (active) {
          setError(caught instanceof ApiError ? caught.message : 'No se pudo cargar el historial.');
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
          <h1>Historial de reservas</h1>
          <p>
            Las reservas rechazadas y canceladas permanecen en el historial; no se eliminan del
            sistema.
          </p>
        </div>
      </header>

      {error && <p className="alert error">{error}</p>}

      <section className="card">
        {loading ? (
          <p className="muted">Cargando historial…</p>
        ) : (
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Espacio</th>
                  <th>Fecha</th>
                  <th>Franja</th>
                  <th>Estado</th>
                  <th>Detalle</th>
                </tr>
              </thead>
              <tbody>
                {reservations.map((reservation) => (
                  <tr key={reservation.id}>
                    <td>{reservation.espacioNombre}</td>
                    <td>{formatDate(reservation.fecha)}</td>
                    <td>
                      {formatTime(reservation.horaInicio)} – {formatTime(reservation.horaFin)}
                    </td>
                    <td>
                      <EstadoBadge estado={reservation.estado} />
                    </td>
                    <td>
                      {reservation.estado === 'Rechazada' && (
                        <span>
                          {reservation.motivoRechazo} · {formatDateTime(reservation.fechaDecision)}
                        </span>
                      )}
                      {reservation.estado === 'Cancelada' && (
                        <span>Cancelada el {formatDateTime(reservation.fechaCancelacion)}</span>
                      )}
                      {reservation.estado === 'Aprobada' && (
                        <span>
                          {reservation.aprobadorNombre} · {formatDateTime(reservation.fechaDecision)}
                        </span>
                      )}
                      {reservation.estado === 'Pendiente' && <span>En espera de decisión</span>}
                    </td>
                  </tr>
                ))}
                {reservations.length === 0 && (
                  <tr>
                    <td colSpan={5} className="muted">
                      No hay registros en el historial.
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
