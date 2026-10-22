export type EstatusCotizacion = 'borrador' | 'enviada' | 'modificada' | 'aceptada' | 'rechazada' | 'vencida';

export interface CotizacionItem {
  id?: number;
  tipo?: string;
  descripcion: string;
  precioUnitario?: number;
  cantidad?: number;
  descuentoItem?: number;
  monto: number;
  notas?: string;
  catalogoServicioId?: number;
  esServicioExtra?: boolean;
  categoriaProveedor?: string;
}

export interface Cotizacion {
  id: number;
  folio: string;
  evento: string;
  catalogoEventoId?: number;
  categoriaEvento?: string;
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
  serviciosPorDefecto?: ServicioDefectoItem[];
}

export interface CrearCotizacionRequest {
  clienteId?: number;
  catalogoEventoId: number;
  fechaEvento: string;
  invitados: number;
  horasAdicionales: number;
  notas?: string;
  estatus?: EstatusCotizacion;
  serviciosExtraIds?: number[];
}

export interface ServicioExtraItem {
  id: number;
  nombre: string;
  descripcion?: string;
  precioBase: number;
  unidad: string;
  categoria?: string;
  categoriaProveedor?: string;
  seleccionado?: boolean;
}

export interface ServicioDefectoItem {
  id: number;
  catalogoServicioId: number;
  nombre: string;
  descripcion?: string;
  categoria: string;
  categoriaProveedor?: string;
  esObligatorio: boolean;
  incluidoEnPrecio: boolean;
  ordenPrioridad: number;
  notas?: string;
}

export interface AprobarCotizacionResponse {
  cotizacionId: number;
  folioCotizacion: string;
  estatusCotizacion: string;
  contratoId: number;
  folioContrato: string;
  eventoContratadoId: number;
  mensaje: string;
}

export interface RechazarCotizacionRequest {
  motivo?: string;
}

