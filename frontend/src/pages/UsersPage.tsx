import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { ApiError, api } from '../api/client';
import type { Rol, User } from '../api/types';

const ROLES: Rol[] = ['Administrador', 'Solicitante', 'Aprobador'];

export function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [editing, setEditing] = useState<User | null>(null);
  const [nombre, setNombre] = useState('');
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [rol, setRol] = useState<Rol>('Solicitante');
  const [activo, setActivo] = useState(true);

  async function load() {
    setLoading(true);
    try {
      setUsers(await api.listUsers());
      setError(null);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo cargar la lista de usuarios.');
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
    setCorreo('');
    setContrasena('');
    setRol('Solicitante');
    setActivo(true);
  }

  function startEdit(user: User) {
    setEditing(user);
    setNombre(user.nombre);
    setCorreo(user.correo);
    setContrasena('');
    setRol(user.rol);
    setActivo(user.activo);
    setError(null);
    setNotice(null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      if (editing) {
        await api.updateUser(editing.id, { nombre, correo, rol, activo });
        setNotice('Usuario actualizado correctamente.');
      } else {
        await api.createUser({ nombre, correo, contrasena, rol });
        setNotice('Usuario registrado correctamente.');
      }
      resetForm();
      await load();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo guardar el usuario.');
    } finally {
      setSaving(false);
    }
  }

  async function toggleState(user: User) {
    setError(null);
    setNotice(null);
    try {
      await api.updateUser(user.id, {
        nombre: user.nombre,
        correo: user.correo,
        rol: user.rol,
        activo: !user.activo
      });
      setNotice(user.activo ? 'Usuario desactivado.' : 'Usuario reactivado.');
      await load();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'No se pudo cambiar el estado del usuario.');
    }
  }

  return (
    <div className="stack">
      <header className="page-header">
        <div>
          <h1>Usuarios</h1>
          <p>Registre usuarios, asigne su rol y controle el acceso al sistema.</p>
        </div>
      </header>

      {error && <p className="alert error">{error}</p>}
      {notice && <p className="alert success">{notice}</p>}

      <section className="card">
        <h2>{editing ? `Editar usuario: ${editing.nombre}` : 'Registrar usuario'}</h2>
        <form onSubmit={handleSubmit} className="form-grid">
          <label className="field">
            <span>Nombre completo</span>
            <input value={nombre} onChange={(event) => setNombre(event.target.value)} required />
          </label>
          <label className="field">
            <span>Correo</span>
            <input
              type="email"
              value={correo}
              onChange={(event) => setCorreo(event.target.value)}
              required
            />
          </label>
          {!editing && (
            <label className="field">
              <span>Contraseña inicial</span>
              <input
                type="password"
                value={contrasena}
                onChange={(event) => setContrasena(event.target.value)}
                minLength={8}
                required
              />
            </label>
          )}
          <label className="field">
            <span>Rol</span>
            <select value={rol} onChange={(event) => setRol(event.target.value as Rol)}>
              {ROLES.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </label>
          {editing && (
            <label className="field checkbox-field">
              <input
                type="checkbox"
                checked={activo}
                onChange={(event) => setActivo(event.target.checked)}
              />
              <span>Usuario activo</span>
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
        <h2>Usuarios registrados</h2>
        {loading ? (
          <p className="muted">Cargando usuarios…</p>
        ) : (
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Correo</th>
                  <th>Rol</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td>{user.nombre}</td>
                    <td>{user.correo}</td>
                    <td>{user.rol}</td>
                    <td>
                      <span className={user.activo ? 'badge estado-aprobada' : 'badge estado-cancelada'}>
                        {user.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="row-actions">
                      <button type="button" className="button small" onClick={() => startEdit(user)}>
                        Editar
                      </button>
                      <button type="button" className="button small ghost" onClick={() => toggleState(user)}>
                        {user.activo ? 'Desactivar' : 'Reactivar'}
                      </button>
                    </td>
                  </tr>
                ))}
                {users.length === 0 && (
                  <tr>
                    <td colSpan={5} className="muted">
                      No hay usuarios registrados.
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
