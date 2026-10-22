import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '@env/environment';
import { Cotizacion, CrearCotizacionRequest } from '@core/models/cotizacion.models';

@Injectable({ providedIn: 'root' })
export class CotizacionesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/cotizaciones`;

  getCotizaciones(clienteId?: number): Observable<Cotizacion[]> {
    const url = clienteId ? `${this.apiUrl}?clienteId=${clienteId}` : this.apiUrl;
    return this.http.get<Cotizacion[]>(url).pipe(
      catchError((err) => {
        console.error('Error fetching cotizaciones:', err);
        return of([]);
      })
    );
  }

  getCotizacionById(id: number): Observable<Cotizacion> {
    return this.http.get<Cotizacion>(`${this.apiUrl}/${id}`);
  }

  crearCotizacion(data: CrearCotizacionRequest): Observable<Cotizacion> {
    return this.http.post<Cotizacion>(this.apiUrl, data);
  }

  actualizarCotizacion(id: number, data: CrearCotizacionRequest): Observable<Cotizacion> {
    return this.http.put<Cotizacion>(`${this.apiUrl}/${id}`, data);
  }

  eliminarCotizacion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  aprobarCotizacion(id: number): Observable<import('@core/models/cotizacion.models').AprobarCotizacionResponse> {
    return this.http.post<import('@core/models/cotizacion.models').AprobarCotizacionResponse>(`${this.apiUrl}/${id}/aprobar`, {});
  }

  rechazarCotizacion(id: number, motivo?: string): Observable<Cotizacion> {
    return this.http.post<Cotizacion>(`${this.apiUrl}/${id}/rechazar`, { motivo });
  }

  getServiciosExtra(categoria?: string): Observable<import('@core/models/cotizacion.models').ServicioExtraItem[]> {
    const url = categoria
      ? `${environment.apiUrl}/api/catalogo-servicios/extras?categoria=${encodeURIComponent(categoria)}`
      : `${environment.apiUrl}/api/catalogo-servicios/extras`;
    return this.http.get<import('@core/models/cotizacion.models').ServicioExtraItem[]>(url).pipe(
      catchError((err) => {
        console.error('Error fetching servicios extra:', err);
        return of([]);
      })
    );
  }

  getServiciosPorDefecto(categoria?: string): Observable<import('@core/models/cotizacion.models').ServicioDefectoItem[]> {
    const url = categoria
      ? `${environment.apiUrl}/api/catalogo-servicios/por-defecto?categoria=${encodeURIComponent(categoria)}`
      : `${environment.apiUrl}/api/catalogo-servicios/por-defecto`;
    return this.http.get<import('@core/models/cotizacion.models').ServicioDefectoItem[]>(url).pipe(
      catchError((err) => {
        console.error('Error fetching servicios por defecto:', err);
        return of([]);
      })
    );
  }
}
