import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarClient } from '@shared/components/navbar-client/navbar-client';
import { EventosService } from '@features/eventos/services/eventos.service';
import { environment } from '@env/environment';

export type EstatusEvento = 'programado' | 'en_curso' | 'completado' | 'cancelado' | 'reprogramado';

export interface EventoContratado {
  id: number;
  nombre: string;
  descripcion: string;
  fechaEvento: string;
  horaInicio: string;
  horaFin: string;
  lugar: string;
  aforo: number;
  estatus: EstatusEvento;
  imagen: string;
  folioContrato: string;
}

@Component({
  selector: 'app-mis-eventos',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarClient],
  templateUrl: './mis-eventos.html'
})
export class MisEventos implements OnInit {
  private readonly eventosService = inject(EventosService);
  private readonly cdr = inject(ChangeDetectorRef);

  busqueda = '';
  filtroEstatus: EstatusEvento | 'todos' = 'todos';
  eventoSeleccionado: EventoContratado | null = null;
  cargando = true;

  eventos: EventoContratado[] = [];

  ngOnInit(): void {
    this.cargarEventos();
  }

  cargarEventos(): void {
    this.cargando = true;
    let clienteId: number | undefined = undefined;
    const sessionStr = localStorage.getItem('session');
    if (sessionStr) {
      try {
        const session = JSON.parse(sessionStr);
        if (session?.cliente?.id) {
          clienteId = session.cliente.id;
        } else if (session?.clienteId) {
          clienteId = session.clienteId;
        }
      } catch (e) {
        console.error('Error al leer sesion:', e);
      }
    }

    this.eventosService.getEventos(clienteId).subscribe({
      next: (data) => {
        if (data && data.length > 0) {
          this.eventos = data.map((ev) => {
            let img = this.getImagenFallback(ev.nombreEvento);
            if (ev.imagenUrl) {
              img = ev.imagenUrl.startsWith('http') ? ev.imagenUrl : `${environment.apiUrl}${ev.imagenUrl}`;
            }
            return {
              id: ev.id,
              nombre: ev.nombreEvento || 'Evento Contratado',
              descripcion: ev.notas || `Evento ${ev.nombreEvento} con contrato ${ev.folioContrato}.`,
              fechaEvento: ev.fechaEvento || '',
              horaInicio: ev.horaInicio || '17:00',
              horaFin: ev.horaFin || '02:00',
              lugar: ev.lugar || 'Salón por definir',
              aforo: ev.aforo || 50,
              estatus: (ev.estatus as EstatusEvento) || 'programado',
              imagen: img,
              folioContrato: ev.folioContrato
            };
          });
        } else {
          this.eventos = [];
        }
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar eventos contratados:', err);
        this.eventos = [];
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  get eventosFiltrados(): EventoContratado[] {
    const termino = this.busqueda.trim().toLowerCase();
    return this.eventos.filter(evento => {
      const coincideBusqueda = !termino ||
        evento.nombre.toLowerCase().includes(termino) ||
        evento.lugar.toLowerCase().includes(termino) ||
        evento.folioContrato.toLowerCase().includes(termino);
      const coincideEstatus = this.filtroEstatus === 'todos' || evento.estatus === this.filtroEstatus;
      return coincideBusqueda && coincideEstatus;
    });
  }

  get totalProgramados(): number {
    return this.eventos.filter(e => e.estatus === 'programado' || e.estatus === 'reprogramado').length;
  }

  get totalCompletados(): number {
    return this.eventos.filter(e => e.estatus === 'completado').length;
  }

  abrirDetalle(evento: EventoContratado): void {
    this.eventoSeleccionado = evento;
  }

  cerrarDetalle(): void {
    this.eventoSeleccionado = null;
  }

  getEstatusLabel(estatus: EstatusEvento): string {
    const labels: Record<EstatusEvento, string> = {
      programado: 'Programado',
      en_curso: 'En curso',
      completado: 'Completado',
      cancelado: 'Cancelado',
      reprogramado: 'Reprogramado'
    };
    return labels[estatus] || estatus;
  }

  getEstatusClasses(estatus: EstatusEvento): string {
    const classes: Record<EstatusEvento, string> = {
      programado: 'bg-blue-100 text-blue-700',
      en_curso: 'bg-yellow-100 text-yellow-700',
      completado: 'bg-green-100 text-green-700',
      cancelado: 'bg-red-100 text-red-700',
      reprogramado: 'bg-purple-100 text-purple-700'
    };
    return classes[estatus] || 'bg-gray-100 text-gray-700';
  }

  formatearFecha(fecha: string): string {
    if (!fecha) return 'Fecha por confirmar';
    return new Date(fecha + 'T12:00:00').toLocaleDateString('es-MX', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  private getImagenFallback(nombre?: string): string {
    const n = (nombre || '').toLowerCase();
    if (n.includes('boda')) {
      return 'https://images.unsplash.com/photo-1519741497674-611481863552?w=600&auto=format&fit=crop&q=80';
    }
    if (n.includes('xv') || n.includes('quince')) {
      return 'https://blogger.googleusercontent.com/img/b/R29vZ2xl/AVvXsEiyRMTv3s0xnDo4619Uh5SBivxzn30mxEqgRRb0Y03suoB99LkglTlmLiEfwk6nlgLEF70Vxearg6R1y9-rXO0effTzdoQKEWd_gdKaUWYfWguZIY9dZ09j_LeN0eyIxgGjKCgKNGq3zRDS/s640/quincea%25C3%25B1era1.jpg';
    }
    if (n.includes('corp') || n.includes('gala') || n.includes('empresa')) {
      return 'https://images.unsplash.com/photo-1511578314322-379afb476865?w=600&auto=format&fit=crop&q=80';
    }
    return 'https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=600&auto=format&fit=crop&q=80';
  }
}
