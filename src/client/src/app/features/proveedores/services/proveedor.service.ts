import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '@env/environment';
import {
  Proveedor,
  CategoriaProveedor,
  ProveedorServicio,
  CrearProveedorRequest,
  ActualizarProveedorRequest,
  CrearProveedorServicioRequest,
  ActualizarProveedorServicioRequest
} from '@core/models/proveedor.model';

@Injectable({ providedIn: 'root' })
export class ProveedorService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/proveedores`;

  getProveedores(categoriaId?: number, incluirInactivos = false): Observable<Proveedor[]> {
    let params = new HttpParams();
    if (categoriaId) params = params.set('categoriaId', categoriaId.toString());
    if (incluirInactivos) params = params.set('incluirInactivos', 'true');

    return this.http.get<Proveedor[]>(this.apiUrl, { params }).pipe(
      catchError((err) => {
        console.error('Error al obtener proveedores:', err);
        return of([]);
      })
    );
  }

  getCategorias(incluirInactivos = false): Observable<CategoriaProveedor[]> {
    const params = new HttpParams().set('incluirInactivos', incluirInactivos.toString());
    return this.http.get<CategoriaProveedor[]>(`${this.apiUrl}/categorias`, { params }).pipe(
      catchError((err) => {
        console.error('Error al obtener categorías de proveedores:', err);
        return of([]);
      })
    );
  }

  getProveedorById(id: number): Observable<Proveedor | null> {
    return this.http.get<Proveedor>(`${this.apiUrl}/${id}`).pipe(
      catchError((err) => {
        console.error(`Error al obtener proveedor ${id}:`, err);
        return of(null);
      })
    );
  }

  crearProveedor(data: CrearProveedorRequest): Observable<Proveedor> {
    return this.http.post<Proveedor>(this.apiUrl, data);
  }

  actualizarProveedor(id: number, data: ActualizarProveedorRequest): Observable<Proveedor> {
    return this.http.put<Proveedor>(`${this.apiUrl}/${id}`, data);
  }

  eliminarProveedor(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getServiciosByProveedorId(proveedorId: number): Observable<ProveedorServicio[]> {
    return this.http.get<ProveedorServicio[]>(`${this.apiUrl}/${proveedorId}/servicios`).pipe(
      catchError((err) => {
        console.error(`Error al obtener servicios del proveedor ${proveedorId}:`, err);
        return of([]);
      })
    );
  }

  crearServicio(proveedorId: number, data: CrearProveedorServicioRequest): Observable<ProveedorServicio> {
    return this.http.post<ProveedorServicio>(`${this.apiUrl}/${proveedorId}/servicios`, data);
  }

  actualizarServicio(servicioId: number, data: ActualizarProveedorServicioRequest): Observable<ProveedorServicio> {
    return this.http.put<ProveedorServicio>(`${this.apiUrl}/servicios/${servicioId}`, data);
  }

  eliminarServicio(servicioId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/servicios/${servicioId}`);
  }
}
