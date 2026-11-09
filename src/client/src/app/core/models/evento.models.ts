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

export interface ContratoPlanPagoItem {
  id: number;
  numeroPago: number;
  concepto: string;
  monto: number;
  fechaVencimiento: string;
  estatus: string;
}

export interface ContratoConceptoItem {
  id: number;
  tipo: string;
  descripcion: string;
  precioUnitario: number;
  cantidad: number;
  descuentoItem: number;
  monto: number;
  notas?: string | null;
  catalogoServicioId?: number | null;
  esServicioExtra?: boolean;
  categoriaProveedor?: string | null;
}

export interface ContratoServicioIncluidoItem {
  id: number;
  catalogoServicioId: number;
  nombre: string;
  descripcion?: string | null;
  categoria: string;
  categoriaProveedor?: string | null;
  esObligatorio: boolean;
  incluidoEnPrecio: boolean;
  ordenPrioridad: number;
  notas?: string | null;
}

export interface ContratoDocumento {
  contratoId: number;
  folioContrato: string;
  cotizacionId?: number | null;
  folioCotizacion?: string | null;
  fechaFirma: string;
  fechaInicio: string;
  fechaFin: string;
  estatusContrato: string;
  subtotalCotizado: number;
  descuento: number;
  totalContrato: number;
  totalPagado: number;
  saldoPendiente: number;
  condicionesEspeciales?: string | null;
  archivoUrl?: string | null;
  clienteId: number;
  clienteNombreCompleto: string;
  clienteEmail: string;
  clienteTelefono?: string | null;
  eventoContratadoId?: number | null;
  nombreEvento: string;
  categoriaEvento?: string | null;
  descripcionEvento?: string | null;
  duracionHorasBase: number;
  capacidadBase: number;
  precioBaseEvento: number;
  fechaEvento: string;
  horaInicio: string;
  horaFin: string;
  lugar: string;
  aforo: number;
  estatusEvento: string;
  notasEvento?: string | null;
  items: ContratoConceptoItem[];
  serviciosIncluidos: ContratoServicioIncluidoItem[];
  planPagos: ContratoPlanPagoItem[];
}

