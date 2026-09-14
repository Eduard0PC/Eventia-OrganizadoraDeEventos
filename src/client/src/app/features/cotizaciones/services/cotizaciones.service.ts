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
}
