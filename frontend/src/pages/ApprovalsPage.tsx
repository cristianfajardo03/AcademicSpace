import { useEffect, useState } from 'react';
import { ApiError, api } from '../api/client';
import type { Reservation } from '../api/types';
import { EstadoBadge } from '../components/EstadoBadge';
import { formatDate, formatTime } from '../utils/format';

export function ApprovalsPage() {
  const [pending, setPending] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [motivos, setMotivos] = useState<Record<string, string>>({});
  const [working, setWorking] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    try {
      setPending(await api.listPending());
      setError(null);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudieron cargar las solicitudes pendientes.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function aprobar(reservation: Reservation) {
    setWorking(reservation.id);
    setError(null);
    setNotice(null);
    try {
      await api.approveReservation(reservation.id);
      setNotice('Solicitud aprobada correctamente.');
      await load();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo aprobar la solicitud.');
    } finally {
      setWorking(null);
    }
  }

  async function rechazar(reservation: Reservation) {
    const motivo = (motivos[reservation.id] ?? '').trim();
    if (!motivo) {
      setError('Debe indicar el motivo del rechazo.');
      return;
    }
    setWorking(reservation.id);
    setError(null);
    setNotice(null);
    try {
      await api.rejectReservation(reservation.id, motivo);
      setNotice('Solicitud rechazada correctamente.');
      setMotivos((current) => ({ ...current, [reservation.id]: '' }));
      await load();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo rechazar la solicitud.');
    } finally {
      setWorking(null);
    }
  }

  return (
    <div className="stack">
      <header className="page-header">
        <div>
          <h1>Solicitudes pendientes</h1>
          <p>Revise cada solicitud y registre la decisión correspondiente.</p>
        </div>
      </header>

      {error && <p className="alert error">{error}</p>}
      {notice && <p className="alert success">{notice}</p>}

      <section className="card">
        <h2>Pendientes ({pending.length})</h2>
        {loading ? (
          <p className="muted">Cargando solicitudes…</p>
        ) : pending.length === 0 ? (
          <p className="empty-state">No hay solicitudes pendientes de decisión.</p>
        ) : (
          <div className="stack">
            {pending.map((reservation) => (
              <article key={reservation.id} className="pending-card">
                <div className="pending-info">
                  <div className="pending-title">
                    <strong>{reservation.espacioNombre}</strong>
                    <EstadoBadge estado={reservation.estado} />
                  </div>
                  <p className="muted">
                    {reservation.solicitanteNombre} · {formatDate(reservation.fecha)} ·{' '}
                    {formatTime(reservation.horaInicio)} – {formatTime(reservation.horaFin)}
                  </p>
                  <p>{reservation.motivo}</p>
                </div>
                <div className="pending-actions">
                  <button
                    type="button"
                    className="button primary small"
                    disabled={working === reservation.id}
                    onClick={() => aprobar(reservation)}
                  >
                    Aprobar
                  </button>
                  <input
                    placeholder="Motivo del rechazo"
                    value={motivos[reservation.id] ?? ''}
                    onChange={(event) =>
                      setMotivos((current) => ({ ...current, [reservation.id]: event.target.value }))
                    }
                  />
                  <button
                    type="button"
                    className="button danger small"
                    disabled={working === reservation.id}
                    onClick={() => rechazar(reservation)}
                  >
                    Rechazar
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
