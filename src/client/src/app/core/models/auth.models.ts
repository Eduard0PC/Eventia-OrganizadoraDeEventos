export interface LoginRequest {
  email: string;
  password: string;
}

export interface ClienteInfo {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  telefono?: string | null;
}

export interface EmpleadoInfo {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  telefono?: string | null;
  cargo?: string | null;
}

export interface LoginResponse {
  id: number;
  email: string;
  rol: string;
  cliente?: ClienteInfo | null;
  empleado?: EmpleadoInfo | null;
}

export interface RegisterRequest {
  email: string;
  password: string;
  nombre: string;
  apellido: string;
  telefono?: string | null;
  rol?: string;
}

export interface UsuarioResponse {
  id: number;
  email: string;
  rol: string;
  activo: boolean;
  clienteId?: number | null;
  empleadoId?: number | null;
  ultimoAcceso?: string | null;
  cliente?: ClienteInfo | null;
  empleado?: EmpleadoInfo | null;
}

export interface ActualizarUsuarioRequest {
  email?: string | null;
  rol?: string | null;
  activo?: boolean | null;
}

export interface CambiarEstadoUsuarioRequest {
  activo: boolean;
}
