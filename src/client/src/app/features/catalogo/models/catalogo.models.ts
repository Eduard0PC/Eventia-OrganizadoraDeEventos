export interface CatalogoEventoItem {
  id: number;
  nombre: string;
  descripcion?: string | null;
  precioBase: number;
  duracionHoras: number;
  categoria?: string;
  capacidadPersonas?: number;
  activo: boolean;
  imagenUrl?: string | null;
  imagen?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CrearCatalogoEventoRequest {
  nombre: string;
  descripcion?: string | null;
  precioBase: number;
  duracionHoras: number;
  categoria?: string;
  capacidadPersonas?: number;
  activo?: boolean;
  imagenUrl?: string | null;
}

export interface ActualizarCatalogoEventoRequest {
  nombre: string;
  descripcion?: string | null;
  precioBase: number;
  duracionHoras: number;
  categoria?: string;
  capacidadPersonas?: number;
  activo?: boolean;
  imagenUrl?: string | null;
}

export interface UploadImagenResponse {
  imageUrl: string;
}
