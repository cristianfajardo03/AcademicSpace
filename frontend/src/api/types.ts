export type Rol = 'Administrador' | 'Solicitante' | 'Aprobador';

export type TipoEspacio = 'Laboratorio' | 'Auditorio' | 'SalaComputo';

export type EstadoReserva = 'Pendiente' | 'Aprobada' | 'Rechazada' | 'Cancelada';

export interface User {
  id: string;
  nombre: string;
  correo: string;
  rol: Rol;
  activo: boolean;
  fechaCreacion: string;
}

export interface LoginResponse {
  token: string;
  expiracion: string;
  usuario: User;
}

export interface Space {
  id: string;
  nombre: string;
  tipo: TipoEspacio;
  ubicacion: string;
  capacidad: number;
  activo: boolean;
}

export interface Reservation {
  id: string;
  espacioId: string;
  espacioNombre: string;
  espacioTipo: TipoEspacio;
  solicitanteId: string;
  solicitanteNombre: string;
  fecha: string;
  horaInicio: string;
  horaFin: string;
  motivo: string;
  estado: EstadoReserva;
  fechaSolicitud: string;
  aprobadorId: string | null;
  aprobadorNombre: string | null;
  fechaDecision: string | null;
  motivoRechazo: string | null;
  fechaCancelacion: string | null;
}

export interface OccupiedInterval {
  horaInicio: string;
  horaFin: string;
}

export interface SpaceAvailability {
  espacioId: string;
  espacioNombre: string;
  disponible: boolean;
  ocupaciones: OccupiedInterval[];
}

export interface AvailableSpaces {
  espacios: Space[];
  mensaje: string;
}

export const TIPOS_ESPACIO: { value: TipoEspacio; label: string; id: number }[] = [
  { value: 'Laboratorio', label: 'Laboratorio', id: 1 },
  { value: 'Auditorio', label: 'Auditorio', id: 2 },
  { value: 'SalaComputo', label: 'Sala de cómputo', id: 3 }
];

export function tipoLabel(tipo: TipoEspacio): string {
  return TIPOS_ESPACIO.find((item) => item.value === tipo)?.label ?? tipo;
}

export function tipoId(tipo: TipoEspacio): number {
  return TIPOS_ESPACIO.find((item) => item.value === tipo)?.id ?? 1;
}
