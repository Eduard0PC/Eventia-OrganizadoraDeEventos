import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { ClienteSummary, ClienteDetalle, CrearClienteRequest } from '../models/cliente.models';

@Injectable({ providedIn: 'root' })
export class ClientesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/clientes`;

  getClientes(): Observable<ClienteSummary[]> {
    return this.http.get<ClienteSummary[]>(this.apiUrl);
  }

  getFichaCliente(id: number): Observable<ClienteDetalle> {
    return this.http.get<ClienteDetalle>(`${this.apiUrl}/${id}/ficha`);
  }

  crearCliente(data: CrearClienteRequest): Observable<ClienteSummary> {
    return this.http.post<ClienteSummary>(this.apiUrl, data);
  }
}
