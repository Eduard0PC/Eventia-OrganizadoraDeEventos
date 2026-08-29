export type EstatusCotizacion = 'borrador' | 'enviada' | 'aceptada' | 'rechazada' | 'vencida';

export interface CotizacionItem {
  id?: number;
  tipo?: string;
  descripcion: string;
  precioUnitario?: number;
  cantidad?: number;
  descuentoItem?: number;
  monto: number;
  notas?: string;
}

export interface Cotizacion {
  id: number;
  folio: string;
  evento: string;
  catalogoEventoId?: number;
  fechaEvento?: string;
  invitados: number;
  total: number;
  descuento: number;
  totalFinal: number;
  estatus: EstatusCotizacion;
  fechaVigencia: string;
  fechaCreacion: string;
  notas: string;
  items: CotizacionItem[];
}

export interface CrearCotizacionRequest {
  clienteId?: number;
  catalogoEventoId: number;
  fechaEvento: string;
  invitados: number;
  horasAdicionales: number;
  notas?: string;
}
