import { ProveedorServicio } from '@core/models/proveedor.model';

export interface ProveedorAsignado {
  id: number;
  proveedorId: number;
  proveedorNombre: string;
  proveedorContacto?: string | null;
  proveedorTelefono?: string | null;
  proveedorEmail?: string | null;
  proveedorDireccion?: string | null;
  rating: number;
  precioPromedio: string;
  capacidadMaxima?: number | null;
  costoAcordado: number;
  notas?: string | null;
  confirmado: boolean;
  fechaAsignacion: string;
}

export interface RequerimientoProveedor {
  claveRequerimiento: string;
  nombreRequerimiento: string;
  descripcion?: string | null;
  categoriaProveedor: string;
  tipoOrigen: 'locacion' | 'paquete' | 'extra' | 'adicional' | string;
  esLocacion: boolean;
  aforoMinimo?: number | null;
  catalogoServicioId?: number | null;
  precioCobradoCliente: number;
  asignacion?: ProveedorAsignado | null;
}

export interface EventoTarea {
  id: number;
  eventoContratadoId: number;
  titulo: string;
  completada: boolean;
  createdAt: string;
}

export interface PlanificacionEvento {
  eventoContratadoId: number;
  contratoId: number;
  folioContrato: string;
  nombreEvento: string;
  categoriaEvento: string;
  clienteId: number;
  clienteNombre: string;
  clienteTelefono?: string | null;
  fechaEvento: string;
  horaInicio: string;
  horaFin: string;
  lugar: string;
  aforo: number;
  estatusEvento: string;
  notasCliente?: string | null;
  totalContrato: number;
  costoTotalProveedores: number;
  margenGanancia: number;
  porcentajeMargen: number;
  totalRequerimientos: number;
  requerimientosCubiertos: number;
  progreso: number;
  requerimientos: RequerimientoProveedor[];
  checklist: EventoTarea[];
}

export interface ProveedorDisponible {
  id: number;
  categoriaId: number;
  categoriaNombre: string;
  nombre: string;
  contacto?: string | null;
  telefono?: string | null;
  email?: string | null;
  direccion?: string | null;
  rating: number;
  precioPromedio: string;
  capacidadMaxima?: number | null;
  costoSugerido: number;
  servicios: ProveedorServicio[];
}

export interface AsignarProveedorEventoRequest {
  claveRequerimiento: string;
  nombreRequerimiento: string;
  categoriaProveedor: string;
  proveedorId: number;
  costoAcordado: number;
  catalogoServicioId?: number | null;
  esLocacion?: boolean;
  notas?: string | null;
}

export interface CrearEventoTareaRequest {
  titulo: string;
}

