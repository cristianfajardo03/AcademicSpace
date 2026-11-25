import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { ApiError, api } from '../api/client';
import type { Space, TipoEspacio } from '../api/types';
import { TIPOS_ESPACIO, tipoId, tipoLabel } from '../api/types';

export function SpacesPage() {
  const [spaces, setSpaces] = useState<Space[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [editing, setEditing] = useState<Space | null>(null);
  const [nombre, setNombre] = useState('');
  const [tipo, setTipo] = useState<TipoEspacio>('Laboratorio');
  const [ubicacion, setUbicacion] = useState('');
  const [capacidad, setCapacidad] = useState(30);
  const [activo, setActivo] = useState(true);

  async function load() {
    setLoading(true);
    try {
      setSpaces(await api.listSpaces());
      setError(null);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo cargar la lista de espacios.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  function resetForm() {
    setEditing(null);
    setNombre('');
    setTipo('Laboratorio');
    setUbicacion('');
    setCapacidad(30);
    setActivo(true);
  }

  function startEdit(space: Space) {
    setEditing(space);
    setNombre(space.nombre);
    setTipo(space.tipo);
    setUbicacion(space.ubicacion);
    setCapacidad(space.capacidad);
    setActivo(space.activo);
    setError(null);
    setNotice(null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      const body = { nombre, tipo: tipoId(tipo), ubicacion, capacidad };
      if (editing) {
        await api.updateSpace(editing.id, { ...body, activo });
        setNotice('Espacio actualizado correctamente.');
      } else {
        await api.createSpace(body);
        setNotice('Espacio registrado correctamente.');
      }
      resetForm();
      await load();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo guardar el espacio.');
    } finally {
      setSaving(false);
    }
  }

  async function toggleState(space: Space) {
    setError(null);
    setNotice(null);
    try {
      await api.updateSpace(space.id, {
        nombre: space.nombre,
        tipo: tipoId(space.tipo),
        ubicacion: space.ubicacion,
        capacidad: space.capacidad,
        activo: !space.activo
      });
      setNotice(space.activo ? 'Espacio desactivado.' : 'Espacio reactivado.');
      await load();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo cambiar el estado del espacio.');
    }
  }

  return (
    <div className="stack">
      <header className="page-header">
        <div>
          <h1>Espacios académicos</h1>
          <p>Los espacios se desactivan; no se eliminan físicamente del sistema.</p>
        </div>
      </header>

      {error && <p className="alert error">{error}</p>}
      {notice && <p className="alert success">{notice}</p>}

      <section className="card">
        <h2>{editing ? `Editar espacio: ${editing.nombre}` : 'Registrar espacio'}</h2>
        <form onSubmit={handleSubmit} className="form-grid">
          <label className="field">
            <span>Nombre</span>
            <input value={nombre} onChange={(event) => setNombre(event.target.value)} required />
          </label>
          <label className="field">
            <span>Tipo</span>
            <select value={tipo} onChange={(event) => setTipo(event.target.value as TipoEspacio)}>
              {TIPOS_ESPACIO.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Ubicación</span>
            <input value={ubicacion} onChange={(event) => setUbicacion(event.target.value)} required />
          </label>
          <label className="field">
            <span>Capacidad</span>
            <input
              type="number"
              min={1}
              value={capacidad}
              onChange={(event) => setCapacidad(Number(event.target.value))}
              required
            />
          </label>
          {editing && (
            <label className="field checkbox-field">
              <input
                type="checkbox"
                checked={activo}
                onChange={(event) => setActivo(event.target.checked)}
              />
              <span>Espacio activo</span>
            </label>
          )}
          <div className="form-actions">
            <button type="submit" className="button primary" disabled={saving}>
              {saving ? 'Guardando…' : editing ? 'Guardar cambios' : 'Registrar'}
            </button>
            {editing && (
              <button type="button" className="button ghost" onClick={resetForm}>
                Cancelar
              </button>
            )}
          </div>
        </form>
      </section>

      <section className="card">
        <h2>Espacios registrados</h2>
        {loading ? (
          <p className="muted">Cargando espacios…</p>
        ) : (
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Tipo</th>
                  <th>Ubicación</th>
                  <th>Capacidad</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {spaces.map((space) => (
                  <tr key={space.id}>
                    <td>{space.nombre}</td>
                    <td>{tipoLabel(space.tipo)}</td>
                    <td>{space.ubicacion}</td>
                    <td>{space.capacidad}</td>
                    <td>
                      <span className={space.activo ? 'badge estado-aprobada' : 'badge estado-cancelada'}>
                        {space.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="row-actions">
                      <button type="button" className="button small" onClick={() => startEdit(space)}>
                        Editar
                      </button>
                      <button type="button" className="button small ghost" onClick={() => toggleState(space)}>
                        {space.activo ? 'Desactivar' : 'Reactivar'}
                      </button>
                    </td>
                  </tr>
                ))}
                {spaces.length === 0 && (
                  <tr>
                    <td colSpan={6} className="muted">
                      No hay espacios registrados.
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
