import type { EstadoReserva } from '../api/types';

export function EstadoBadge({ estado }: { estado: EstadoReserva }) {
  return <span className={`badge estado-${estado.toLowerCase()}`}>{estado}</span>;
}
