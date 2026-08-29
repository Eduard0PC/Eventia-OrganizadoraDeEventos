export interface ClienteSummary {
  id: number;
  nombre: string;
  apellido: string;
  nombreCompleto: string;
  email: string;
  telefono: string | null;
  activo: boolean;
  estatusLabel: string;
  ultimoEventoContratado: string;
  fechaUltimoEvento?: string | null;
  totalEventos: number;
}

export interface PagoCliente {
  id: number;
  folioContrato: string;
  monto: number;
  metodoPago: string;
  tipoTransaccion: string;
  fechaPago: string;
  estatus: string;
  referencia: string | null;
}

export interface ServicioContratado {
  id: number;
  nombre: string;
  tipo: string;
  precioUnitario: number;
  cantidad: number;
  subtotal: number;
  fechaCotizacion: string;
  folioCotizacion: string;
}

export interface EventoActivo {
  id: number;
  nombreEvento: string;
  fechaEvento: string;
  lugar: string | null;
  aforo: number | null;
  estatus: string;
  folioContrato: string;
}

export interface ClienteDetalle {
  id: number;
  nombre: string;
  apellido: string;
  nombreCompleto: string;
  email: string;
  telefono: string | null;
  activo: boolean;
  estatusLabel: string;
  ultimoEventoContratado: string;
  totalEventos: number;
  totalPagado: number;
  historialPagos: PagoCliente[];
  serviciosContratados: ServicioContratado[];
  eventosActivos: EventoActivo[];
}

export interface CrearClienteRequest {
  nombre: string;
  apellido: string;
  email: string;
  telefono?: string;
  password?: string;
}
