import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '@env/environment';
import { EventoContratadoItem } from '@core/models/evento.models';

@Injectable({ providedIn: 'root' })
export class EventosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/eventos-contratados`;

  getEventos(clienteId?: number): Observable<EventoContratadoItem[]> {
    const url = clienteId ? `${this.apiUrl}?clienteId=${clienteId}` : this.apiUrl;
    return this.http.get<EventoContratadoItem[]>(url).pipe(
      catchError((err) => {
        console.error('Error fetching eventos contratados:', err);
        return of([]);
      })
    );
  }

  getEventoById(id: number): Observable<EventoContratadoItem> {
    return this.http.get<EventoContratadoItem>(`${this.apiUrl}/${id}`);
  }

  actualizarEstatus(id: number, estatus: string, notas?: string): Observable<EventoContratadoItem> {
    return this.http.put<EventoContratadoItem>(`${this.apiUrl}/${id}/estatus`, {
      estatus,
      notas
    });
  }
}
