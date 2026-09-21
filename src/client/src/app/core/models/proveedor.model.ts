export interface CategoriaProveedor {
  id: number;
  nombre: string;
  descripcion?: string;
  activo: boolean;
}

export interface ProveedorServicio {
  id: number;
  proveedorId: number;
  nombreServicio: string;
  descripcion?: string;
  costo: number;
  unidad: string;
  activo: boolean;
}

export interface Proveedor {
  id: number;
  categoriaId: number;
  categoriaNombre: string;
  nombre: string;
  contacto?: string;
  email?: string;
  telefono?: string;
  direccion?: string;
  sitioWeb?: string;
  rfc?: string;
  rating: number;
  precioPromedio: string;
  activo: boolean;
  createdAt?: string;
  servicios: ProveedorServicio[];
}

export interface CrearProveedorRequest {
  categoriaId: number;
  nombre: string;
  contacto?: string;
  email?: string;
  telefono?: string;
  direccion?: string;
  sitioWeb?: string;
  rfc?: string;
  rating?: number;
  precioPromedio?: string;
  activo?: boolean;
}

export interface ActualizarProveedorRequest {
  categoriaId: number;
  nombre: string;
  contacto?: string;
  email?: string;
  telefono?: string;
  direccion?: string;
  sitioWeb?: string;
  rfc?: string;
  rating?: number;
  precioPromedio?: string;
  activo?: boolean;
}

export interface CrearProveedorServicioRequest {
  nombreServicio: string;
  descripcion?: string;
  costo: number;
  unidad?: string;
  activo?: boolean;
}

export interface ActualizarProveedorServicioRequest {
  nombreServicio: string;
  descripcion?: string;
  costo: number;
  unidad?: string;
  activo?: boolean;
}
