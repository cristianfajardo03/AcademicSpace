import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { api, clearSession, getStoredUser, getToken, storeSession, UNAUTHORIZED_EVENT } from '../api/client';
import type { User } from '../api/types';

interface AuthContextValue {
  user: User | null;
  loading: boolean;
  login: (correo: string, contrasena: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => getStoredUser());
  const [loading, setLoading] = useState<boolean>(() => Boolean(getToken()));

  useEffect(() => {
    let active = true;

    async function validateSession() {
      if (!getToken()) {
        setLoading(false);
        return;
      }
      try {
        const current = await api.me();
        if (active) {
          storeSession(getToken()!, current);
          setUser(current);
        }
      } catch {
        if (active) {
          clearSession();
          setUser(null);
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void validateSession();

    const onUnauthorized = () => setUser(null);
    window.addEventListener(UNAUTHORIZED_EVENT, onUnauthorized);
    return () => {
      active = false;
      window.removeEventListener(UNAUTHORIZED_EVENT, onUnauthorized);
    };
  }, []);

  const login = useCallback(async (correo: string, contrasena: string) => {
    const response = await api.login(correo, contrasena);
    storeSession(response.token, response.usuario);
    setUser(response.usuario);
  }, []);

  const logout = useCallback(async () => {
    try {
      await api.logout();
    } catch {
      // La sesión local se cierra aunque el servidor no responda.
    } finally {
      clearSession();
      setUser(null);
    }
  }, []);

  const value = useMemo(() => ({ user, loading, login, logout }), [user, loading, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth debe usarse dentro de AuthProvider.');
  }
  return context;
}
