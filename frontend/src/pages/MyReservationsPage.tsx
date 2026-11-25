import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { ApiError, api } from '../api/client';
import type { Reservation, Space } from '../api/types';
import { tipoLabel } from '../api/types';
import { EstadoBadge } from '../components/EstadoBadge';
import { formatDate, formatTime, tomorrow } from '../utils/format';

export function MyReservationsPage() {
  const [spaces, setSpaces] = useState<Space[]>([]);
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [espacioId, setEspacioId] = useState('');
  const [fecha, setFecha] = useState(tomorrow());
  const [horaInicio, setHoraInicio] = useState('08:00');
  const [horaFin, setHoraFin] = useState('10:00');
  const [motivo, setMotivo] = useState('');

  async function load() {
    setLoading(true);
    try {
      const [spaceList, reservationList] = await Promise.all([api.listSpaces(), api.listReservations()]);
      setSpaces(spaceList);
      setReservations(reservationList);
      setEspacioId((current) => current || spaceList[0]?.id || '');
      setError(null);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudieron cargar las solicitudes.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await api.createReservation({ espacioId, fecha, horaInicio, horaFin, motivo });
      setNotice('Solicitud registrada en estado pendiente.');
      setMotivo('');
      await load();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo registrar la solicitud.');
    } finally {
      setSaving(false);
    }
  }

  async function cancelar(reservation: Reservation) {
    setError(null);
    setNotice(null);
    try {
      await api.cancelReservation(reservation.id);
      setNotice('Reserva cancelada. La franja quedó liberada.');
      await load();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo cancelar la reserva.');
    }
  }

  return (
    <div className="stack">
      <header className="page-header">
        <div>
          <h1>Mis solicitudes de reserva</h1>
          <p>Cree solicitudes para espacios activos y cancele las reservas que ya no necesite.</p>
        </div>
      </header>

      {error && <p className="alert error">{error}</p>}
      {notice && <p className="alert success">{notice}</p>}

      <section className="card">
        <h2>Nueva solicitud</h2>
        <form onSubmit={handleSubmit} className="form-grid">
          <label className="field">
            <span>Espacio</span>
            <select value={espacioId} onChange={(event) => setEspacioId(event.target.value)} required>
              {spaces.map((space) => (
                <option key={space.id} value={space.id}>
                  {space.nombre} · {tipoLabel(space.tipo)}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Fecha</span>
            <input type="date" value={fecha} onChange={(event) => setFecha(event.target.value)} required />
          </label>
          <label className="field">
            <span>Hora de inicio</span>
            <input
              type="time"
              value={horaInicio}
              onChange={(event) => setHoraInicio(event.target.value)}
              required
            />
          </label>
          <label className="field">
            <span>Hora de fin</span>
            <input type="time" value={horaFin} onChange={(event) => setHoraFin(event.target.value)} required />
          </label>
          <label className="field wide">
            <span>Motivo</span>
            <input
              value={motivo}
              onChange={(event) => setMotivo(event.target.value)}
              maxLength={500}
              required
            />
          </label>
          <div className="form-actions">
            <button type="submit" className="button primary" disabled={saving || spaces.length === 0}>
              {saving ? 'Enviando…' : 'Solicitar reserva'}
            </button>
          </div>
        </form>
        {spaces.length === 0 && <p className="muted">No hay espacios activos disponibles para solicitar.</p>}
      </section>

      <section className="card">
        <h2>Mis solicitudes</h2>
        {loading ? (
          <p className="muted">Cargando solicitudes…</p>
        ) : (
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Espacio</th>
                  <th>Fecha</th>
                  <th>Franja</th>
                  <th>Motivo</th>
                  <th>Estado</th>
                  <th>Acciones</th>
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
                    <td>{reservation.motivo}</td>
                    <td>
                      <EstadoBadge estado={reservation.estado} />
                    </td>
                    <td>
                      {(reservation.estado === 'Pendiente' || reservation.estado === 'Aprobada') && (
                        <button type="button" className="button small ghost" onClick={() => cancelar(reservation)}>
                          Cancelar
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {reservations.length === 0 && (
                  <tr>
                    <td colSpan={6} className="muted">
                      Aún no ha registrado solicitudes.
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
