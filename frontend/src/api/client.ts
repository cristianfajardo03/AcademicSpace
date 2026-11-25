import type { AvailableSpaces, LoginResponse, Reservation, Space, SpaceAvailability, User } from './types';

const API_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5199';

const TOKEN_KEY = 'academicspace.token';
const USER_KEY = 'academicspace.user';

export const UNAUTHORIZED_EVENT = 'academicspace:unauthorized';

export class ApiError extends Error {
  status: number;
  fieldErrors: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

export function getToken(): string | null {
  return sessionStorage.getItem(TOKEN_KEY);
}

export function getStoredUser(): User | null {
  const raw = sessionStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as User;
  } catch {
    return null;
  }
}

export function storeSession(token: string, user: User): void {
  sessionStorage.setItem(TOKEN_KEY, token);
  sessionStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearSession(): void {
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(USER_KEY);
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  const token = getToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  let response: Response;
  try {
    response = await fetch(`${API_URL}${path}`, { ...options, headers });
  } catch {
    throw new ApiError(0, 'No se pudo conectar con el servidor. Verifique que la API esté en ejecución.');
  }

  if (response.status === 401) {
    clearSession();
    window.dispatchEvent(new Event(UNAUTHORIZED_EVENT));
    throw new ApiError(401, 'La sesión expiró o no es válida. Inicie sesión nuevamente.');
  }

  if (!response.ok) {
    let problem: ProblemDetails = {};
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = {};
    }
    const message =
      problem.detail ??
      problem.title ??
      (response.status === 403 ? 'No tiene permisos para realizar esta acción.' : 'Ocurrió un error inesperado.');
    throw new ApiError(response.status, message, problem.errors ?? {});
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

function jsonBody(value: unknown): string {
  return JSON.stringify(value);
}

function normalizeTime(value: string): string {
  return value.length === 5 ? `${value}:00` : value;
}

export const api = {
  login: (correo: string, contrasena: string) =>
    request<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: jsonBody({ correo, contrasena })
    }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),

  me: () => request<User>('/api/users/me'),
  updateMe: (body: { nombre: string; correo: string }) =>
    request<User>('/api/users/me', { method: 'PUT', body: jsonBody(body) }),

  listUsers: () => request<User[]>('/api/users'),
  createUser: (body: { nombre: string; correo: string; contrasena: string; rol: string }) =>
    request<User>('/api/users', { method: 'POST', body: jsonBody(body) }),
  updateUser: (id: string, body: { nombre: string; correo: string; rol: string; activo: boolean }) =>
    request<User>(`/api/users/${id}`, { method: 'PUT', body: jsonBody(body) }),

  listSpaces: () => request<Space[]>('/api/spaces'),
  createSpace: (body: { nombre: string; tipo: number; ubicacion: string; capacidad: number }) =>
    request<Space>('/api/spaces', { method: 'POST', body: jsonBody(body) }),
  updateSpace: (
    id: string,
    body: { nombre: string; tipo: number; ubicacion: string; capacidad: number; activo: boolean }
  ) => request<Space>(`/api/spaces/${id}`, { method: 'PUT', body: jsonBody(body) }),

  availability: (params: { fecha: string; horaInicio: string; horaFin: string; tipo?: number }) => {
    const query = new URLSearchParams({
      fecha: params.fecha,
      horaInicio: params.horaInicio,
      horaFin: params.horaFin
    });
    if (params.tipo) {
      query.set('tipo', String(params.tipo));
    }
    return request<AvailableSpaces>(`/api/availability/spaces?${query.toString()}`);
  },
  spaceAvailability: (id: string, params: { fecha: string; horaInicio: string; horaFin: string }) => {
    const query = new URLSearchParams({
      fecha: params.fecha,
      horaInicio: params.horaInicio,
      horaFin: params.horaFin
    });
    return request<SpaceAvailability>(
      `/api/availability/spaces/${id}?${query.toString()}`
    );
  },

  listReservations: () => request<Reservation[]>('/api/reservations'),
  listPending: () => request<Reservation[]>('/api/reservations/pending'),
  history: () => request<Reservation[]>('/api/reservations/history'),
  createReservation: (body: {
    espacioId: string;
    fecha: string;
    horaInicio: string;
    horaFin: string;
    motivo: string;
  }) =>
    request<Reservation>('/api/reservations', {
      method: 'POST',
      body: jsonBody({ ...body, horaInicio: normalizeTime(body.horaInicio), horaFin: normalizeTime(body.horaFin) })
    }),
  approveReservation: (id: string) =>
    request<Reservation>(`/api/reservations/${id}/approve`, { method: 'POST' }),
  rejectReservation: (id: string, motivo: string) =>
    request<Reservation>(`/api/reservations/${id}/reject`, {
      method: 'POST',
      body: jsonBody({ motivo })
    }),
  cancelReservation: (id: string) =>
    request<Reservation>(`/api/reservations/${id}/cancel`, { method: 'POST' })
};
