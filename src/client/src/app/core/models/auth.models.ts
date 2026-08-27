export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  id: number;
  email: string;
  rol: string;
  cliente: {
    id: number;
    nombre: string;
    apellido: string;
    email: string;
    telefono?: string | null;
  } | null;
}
