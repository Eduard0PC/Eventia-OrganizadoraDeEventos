export type EstatusEvento = 'programado' | 'en_curso' | 'completado' | 'cancelado' | 'reprogramado';

export interface EventoContratadoItem {
  id: number;
  contratoId: number;
  folioContrato: string;
  catalogoEventoId: number;
  nombreEvento: string;
  categoriaEvento?: string;
  clienteId: number;
  nombreCliente: string;
  fechaEvento?: string;
  horaInicio?: string;
  horaFin?: string;
  lugar?: string;
  aforo?: number;
  estatus: EstatusEvento;
  notas?: string;
  totalContrato: number;
  imagenUrl?: string;
}
