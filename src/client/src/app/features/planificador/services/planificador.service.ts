import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  PlanificacionEvento,
  ProveedorDisponible,
  AsignarProveedorEventoRequest,
  EventoTarea,
  CrearEventoTareaRequest,
} from '../models/planificador.models';

@Injectable({
  providedIn: 'root',
})
export class PlanificadorService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/planificador`;

  getPlanificaciones(): Observable<PlanificacionEvento[]> {
    return this.http.get<PlanificacionEvento[]>(`${this.baseUrl}/eventos`);
  }

  getPlanificacionByEventoId(eventoId: number): Observable<PlanificacionEvento> {
    return this.http.get<PlanificacionEvento>(`${this.baseUrl}/eventos/${eventoId}`);
  }

  getProveedoresDisponibles(
    eventoId: number,
    categoria?: string,
    esLocacion = false
  ): Observable<ProveedorDisponible[]> {
    let params = new HttpParams().set('esLocacion', String(esLocacion));
    if (categoria) {
      params = params.set('categoria', categoria);
    }
    return this.http.get<ProveedorDisponible[]>(
      `${this.baseUrl}/eventos/${eventoId}/proveedores-disponibles`,
      { params }
    );
  }

  asignarProveedor(
    eventoId: number,
    payload: AsignarProveedorEventoRequest
  ): Observable<PlanificacionEvento> {
    return this.http.post<PlanificacionEvento>(
      `${this.baseUrl}/eventos/${eventoId}/asignaciones`,
      payload
    );
  }

  removerAsignacion(eventoId: number, asignacionId: number): Observable<PlanificacionEvento> {
    return this.http.delete<PlanificacionEvento>(
      `${this.baseUrl}/eventos/${eventoId}/asignaciones/${asignacionId}`
    );
  }

  crearTarea(eventoId: number, payload: CrearEventoTareaRequest): Observable<EventoTarea> {
    return this.http.post<EventoTarea>(
      `${this.baseUrl}/eventos/${eventoId}/tareas`,
      payload
    );
  }

  toggleTarea(eventoId: number, tareaId: number): Observable<EventoTarea> {
    return this.http.patch<EventoTarea>(
      `${this.baseUrl}/eventos/${eventoId}/tareas/${tareaId}/toggle`,
      {}
    );
  }

  eliminarTarea(eventoId: number, tareaId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/eventos/${eventoId}/tareas/${tareaId}`
    );
  }
}

