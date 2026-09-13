import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  CatalogoEventoItem,
  CrearCatalogoEventoRequest,
  ActualizarCatalogoEventoRequest,
  UploadImagenResponse,
} from '../models/catalogo.models';

@Injectable({ providedIn: 'root' })
export class CatalogoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/catalogo-eventos`;

  getCatalogoEventos(incluirInactivos = false): Observable<CatalogoEventoItem[]> {
    const url = incluirInactivos ? `${this.apiUrl}?incluirInactivos=true` : this.apiUrl;
    return this.http.get<CatalogoEventoItem[]>(url);
  }

  getCatalogoEventoById(id: number): Observable<CatalogoEventoItem> {
    return this.http.get<CatalogoEventoItem>(`${this.apiUrl}/${id}`);
  }

  crearCatalogoEvento(data: CrearCatalogoEventoRequest): Observable<CatalogoEventoItem> {
    return this.http.post<CatalogoEventoItem>(this.apiUrl, data);
  }

  actualizarCatalogoEvento(id: number, data: ActualizarCatalogoEventoRequest): Observable<CatalogoEventoItem> {
    return this.http.put<CatalogoEventoItem>(`${this.apiUrl}/${id}`, data);
  }

  eliminarCatalogoEvento(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  subirImagenEvento(file: File): Observable<UploadImagenResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<UploadImagenResponse>(`${this.apiUrl}/upload-imagen`, formData);
  }
}

