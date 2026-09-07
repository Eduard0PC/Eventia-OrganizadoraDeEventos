import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap, timeout } from 'rxjs';
import { environment } from '@env/environment';
import {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  UsuarioResponse,
  ActualizarUsuarioRequest,
} from '@core/models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/auth`;
  private readonly usuariosUrl = `${environment.apiUrl}/api/usuarios`;

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      timeout(10000),
      tap((session) => localStorage.setItem('session', JSON.stringify(session))),
    );
  }

  register(data: RegisterRequest): Observable<UsuarioResponse> {
    return this.http.post<UsuarioResponse>(`${this.apiUrl}/register`, data).pipe(
      timeout(10000)
    );
  }

  // ==========================================
  // GESTIÓN DE USUARIOS
  // ==========================================

  getUsuarios(): Observable<UsuarioResponse[]> {
    return this.http.get<UsuarioResponse[]>(this.usuariosUrl);
  }

  getUsuarioById(id: number): Observable<UsuarioResponse> {
    return this.http.get<UsuarioResponse>(`${this.usuariosUrl}/${id}`);
  }

  actualizarUsuario(id: number, request: ActualizarUsuarioRequest): Observable<UsuarioResponse> {
    return this.http.put<UsuarioResponse>(`${this.usuariosUrl}/${id}`, request);
  }

  cambiarEstadoUsuario(id: number, activo: boolean): Observable<void> {
    return this.http.patch<void>(`${this.usuariosUrl}/${id}/estado`, { activo });
  }
}

