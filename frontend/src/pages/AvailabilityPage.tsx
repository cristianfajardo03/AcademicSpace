import { useState } from 'react';
import type { FormEvent } from 'react';
import { ApiError, api } from '../api/client';
import type { AvailableSpaces, Space, SpaceAvailability, TipoEspacio } from '../api/types';
import { TIPOS_ESPACIO, tipoId, tipoLabel } from '../api/types';
import { formatDate, formatTime, tomorrow } from '../utils/format';

export function AvailabilityPage() {
  const [fecha, setFecha] = useState(tomorrow());
  const [horaInicio, setHoraInicio] = useState('08:00');
  const [horaFin, setHoraFin] = useState('10:00');
  const [tipo, setTipo] = useState<'' | TipoEspacio>('');

  const [result, setResult] = useState<AvailableSpaces | null>(null);
  const [detail, setDetail] = useState<SpaceAvailability | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function consultar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    setDetail(null);
    try {
      setResult(
        await api.availability({
          fecha,
          horaInicio,
          horaFin,
          tipo: tipo ? tipoId(tipo) : undefined
        })
      );
    } catch (caught) {
      setResult(null);
      setError(caught instanceof ApiError ? caught.message : 'No se pudo consultar la disponibilidad.');
    } finally {
      setLoading(false);
    }
  }

  async function verFranjas(space: Space) {
    setError(null);
    try {
      setDetail(await api.spaceAvailability(space.id, { fecha, horaInicio, horaFin }));
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo consultar el detalle del espacio.');
    }
  }

  return (
    <div className="stack">
      <header className="page-header">
        <div>
          <h1>Disponibilidad de espacios</h1>
          <p>
            Una franja está ocupada cuando se superpone con una reserva pendiente o aprobada. Las
            reservas rechazadas y canceladas no bloquean el espacio.
          </p>
        </div>
      </header>

      {error && <p className="alert error">{error}</p>}

      <section className="card">
        <form onSubmit={consultar} className="form-grid compact">
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
          <label className="field">
            <span>Tipo de espacio</span>
            <select value={tipo} onChange={(event) => setTipo(event.target.value as '' | TipoEspacio)}>
              <option value="">Todos</option>
              {TIPOS_ESPACIO.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <div className="form-actions">
            <button type="submit" className="button primary" disabled={loading}>
              {loading ? 'Consultando…' : 'Consultar'}
            </button>
          </div>
        </form>
      </section>

      {result && (
        <section className="card">
          <h2>
            Espacios disponibles · {formatDate(fecha)} de {formatTime(horaInicio)} a {formatTime(horaFin)}
          </h2>
          <p className="muted">{result.mensaje}</p>
          {result.espacios.length > 0 ? (
            <div className="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Espacio</th>
                    <th>Tipo</th>
                    <th>Ubicación</th>
                    <th>Capacidad</th>
                    <th>Franjas</th>
                  </tr>
                </thead>
                <tbody>
                  {result.espacios.map((space) => (
                    <tr key={space.id}>
                      <td>{space.nombre}</td>
                      <td>{tipoLabel(space.tipo)}</td>
                      <td>{space.ubicacion}</td>
                      <td>{space.capacidad}</td>
                      <td>
                        <button type="button" className="button small" onClick={() => verFranjas(space)}>
                          Ver franjas
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="empty-state">No hay espacios disponibles en la franja consultada.</p>
          )}
        </section>
      )}

      {detail && (
        <section className="card">
          <h2>Franjas ocupadas · {detail.espacioNombre}</h2>
          <p>
            Estado:{' '}
            <span className={detail.disponible ? 'badge estado-aprobada' : 'badge estado-pendiente'}>
              {detail.disponible ? 'Disponible' : 'Ocupado'}
            </span>
          </p>
          {detail.ocupaciones.length > 0 ? (
            <ul className="interval-list">
              {detail.ocupaciones.map((interval, index) => (
                <li key={`${interval.horaInicio}-${interval.horaFin}-${index}`}>
                  {formatTime(interval.horaInicio)} – {formatTime(interval.horaFin)}
                </li>
              ))}
            </ul>
          ) : (
            <p className="muted">Sin ocupaciones en la franja consultada.</p>
          )}
        </section>
      )}
    </div>
  );
}
