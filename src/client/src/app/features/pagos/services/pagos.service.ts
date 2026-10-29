import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '@env/environment';
import {
  PagoItem,
  CuotaPendiente,
  ResumenPagos,
  EstadoCuentaEvento,
  RegistrarPagoRequest,
  RegistrarPagoResponse
} from '@core/models/pago.models';

@Injectable({ providedIn: 'root' })
export class PagosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/pagos`;

  getEstadoCuenta(clienteId?: number, contratoId?: number): Observable<EstadoCuentaEvento[]> {
    let params = new HttpParams();
    if (clienteId) params = params.set('clienteId', clienteId.toString());
    if (contratoId) params = params.set('contratoId', contratoId.toString());

    return this.http.get<EstadoCuentaEvento[]>(`${this.apiUrl}/estado-cuenta`, { params }).pipe(
      catchError((err) => {
        console.error('Error fetching estado de cuenta:', err);
        return of([]);
      })
    );
  }

  getPagos(clienteId?: number, contratoId?: number): Observable<PagoItem[]> {
    let params = new HttpParams();
    if (clienteId) params = params.set('clienteId', clienteId.toString());
    if (contratoId) params = params.set('contratoId', contratoId.toString());

    return this.http.get<PagoItem[]>(this.apiUrl, { params }).pipe(
      catchError((err) => {
        console.error('Error fetching pagos:', err);
        return of([]);
      })
    );
  }

  getCuotasPendientes(clienteId?: number, contratoId?: number): Observable<CuotaPendiente[]> {
    let params = new HttpParams();
    if (clienteId) params = params.set('clienteId', clienteId.toString());
    if (contratoId) params = params.set('contratoId', contratoId.toString());

    return this.http.get<CuotaPendiente[]>(`${this.apiUrl}/cuotas-pendientes`, { params }).pipe(
      catchError((err) => {
        console.error('Error fetching cuotas pendientes:', err);
        return of([]);
      })
    );
  }

  getResumen(clienteId?: number): Observable<ResumenPagos> {
    let params = new HttpParams();
    if (clienteId) params = params.set('clienteId', clienteId.toString());

    return this.http.get<ResumenPagos>(`${this.apiUrl}/resumen`, { params }).pipe(
      catchError((err) => {
        console.error('Error fetching resumen de pagos:', err);
        return of({
          totalPagado: 0,
          totalPendiente: 0,
          proximoPago: null,
          totalContratos: 0,
          conteoPagos: 0,
          conteoCuotasPendientes: 0
        });
      })
    );
  }

  registrarPago(request: RegistrarPagoRequest): Observable<RegistrarPagoResponse> {
    return this.http.post<RegistrarPagoResponse>(this.apiUrl, request);
  }

  generarPlanPagos(contratoId: number): Observable<{ mensaje: string }> {
    return this.http.post<{ mensaje: string }>(`${environment.apiUrl}/api/contratos/${contratoId}/generar-plan-pagos`, {});
  }
}
