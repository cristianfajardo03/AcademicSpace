import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { user, loading, login } = useAuth();
  const navigate = useNavigate();
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (!loading && user) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(correo.trim(), contrasena);
      navigate('/', { replace: true });
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 401) {
        setError('Credenciales inválidas o cuenta no habilitada.');
      } else if (caught instanceof ApiError) {
        setError(caught.message);
      } else {
        setError('No se pudo iniciar sesión. Intente nuevamente.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="login-shell">
      <div className="login-card">
        <div className="login-brand">
          <span className="brand-mark large">AS</span>
          <h1>AcademicSpace</h1>
          <p>Gestión de reservas de espacios académicos</p>
        </div>

        <form onSubmit={handleSubmit} className="stack">
          <label className="field">
            <span>Correo institucional</span>
            <input
              type="email"
              value={correo}
              onChange={(event) => setCorreo(event.target.value)}
              autoComplete="username"
              required
            />
          </label>
          <label className="field">
            <span>Contraseña</span>
            <input
              type="password"
              value={contrasena}
              onChange={(event) => setContrasena(event.target.value)}
              autoComplete="current-password"
              required
            />
          </label>

          {error && <p className="alert error">{error}</p>}

          <button type="submit" className="button primary" disabled={submitting}>
            {submitting ? 'Ingresando…' : 'Iniciar sesión'}
          </button>
        </form>

        <p className="login-help">
          El acceso se crea únicamente a través del administrador del sistema.
        </p>
      </div>
    </div>
  );
}
