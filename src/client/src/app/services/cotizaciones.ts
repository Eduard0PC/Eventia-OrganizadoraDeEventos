import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type EstatusCotizacion = 'borrador' | 'enviada' | 'aceptada' | 'rechazada' | 'vencida';

export interface CotizacionItem {
  id?: number;
  descripcion: string;
  monto: number;
}

export interface Cotizacion {
  id: number;
  folio: string;
  evento: string;
  catalogoEventoId?: number;
  fechaEvento?: string;
  invitados: number;
  total: number;
  descuento: number;
  totalFinal: number;
  estatus: EstatusCotizacion;
  fechaVigencia: string;
  fechaCreacion: string;
  notas: string;
  items: CotizacionItem[];
}

export interface CrearCotizacionRequest {
  clienteId?: number;
  catalogoEventoId: number;
  fechaEvento: string;
  invitados: number;
  horasAdicionales: number;
  notas?: string;
}

@Injectable({ providedIn: 'root' })
export class CotizacionesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/cotizaciones`;

  getCotizaciones(clienteId?: number): Observable<Cotizacion[]> {
    const url = clienteId ? `${this.apiUrl}?clienteId=${clienteId}` : this.apiUrl;
    return this.http.get<Cotizacion[]>(url);
  }

  getCotizacionById(id: number): Observable<Cotizacion> {
    return this.http.get<Cotizacion>(`${this.apiUrl}/${id}`);
  }

  crearCotizacion(data: CrearCotizacionRequest): Observable<Cotizacion> {
    return this.http.post<Cotizacion>(this.apiUrl, data);
  }

  eliminarCotizacion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
