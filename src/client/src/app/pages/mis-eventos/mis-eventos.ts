import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarClient } from '../../components/navbar-client/navbar-client';

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
export class MisEventos {
  busqueda = '';
  filtroEstatus: EstatusEvento | 'todos' = 'todos';
  eventoSeleccionado: EventoContratado | null = null;

  readonly eventos: EventoContratado[] = [
    {
      id: 1,
      nombre: 'Boda premium',
      descripcion: 'Evento nupcial para hasta 300 personas con servicio de banquete y DJ en vivo.',
      fechaEvento: '2026-08-15',
      horaInicio: '17:00',
      horaFin: '03:00',
      lugar: 'Salón Jardines del Valle, Guadalajara',
      aforo: 280,
      estatus: 'programado',
      imagen: 'https://images.unsplash.com/photo-1519741497674-611481863552?w=600&auto=format&fit=crop&q=80',
      folioContrato: 'CTR-2026-0042'
    },
    {
      id: 2,
      nombre: 'XV años',
      descripcion: 'Celebración de quinceañera con decoración temática y música en vivo.',
      fechaEvento: '2026-05-20',
      horaInicio: '19:00',
      horaFin: '01:00',
      lugar: 'Salón Crystal, Zapopan',
      aforo: 150,
      estatus: 'completado',
      imagen: 'https://blogger.googleusercontent.com/img/b/R29vZ2xl/AVvXsEiyRMTv3s0xnDo4619Uh5SBivxzn30mxEqgRRb0Y03suoB99LkglTlmLiEfwk6nlgLEF70Vxearg6R1y9-rXO0effTzdoQKEWd_gdKaUWYfWguZIY9dZ09j_LeN0eyIxgGjKCgKNGq3zRDS/s640/quincea%25C3%25B1era1.jpg',
      folioContrato: 'CTR-2025-0187'
    },
    {
      id: 3,
      nombre: 'Evento corporativo',
      descripcion: 'Conferencia anual de la empresa con coffee break y equipo audiovisual.',
      fechaEvento: '2026-07-10',
      horaInicio: '09:00',
      horaFin: '14:00',
      lugar: 'Centro de Convenciones Expo Guadalajara',
      aforo: 80,
      estatus: 'reprogramado',
      imagen: 'https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=600&auto=format&fit=crop&q=80',
      folioContrato: 'CTR-2026-0031'
    },
    {
      id: 4,
      nombre: 'Graduación',
      descripcion: 'Celebración de graduación universitaria con banquete y ceremonia.',
      fechaEvento: '2026-06-28',
      horaInicio: '18:30',
      horaFin: '23:00',
      lugar: 'Hotel Real de Minas, Guadalajara',
      aforo: 120,
      estatus: 'programado',
      imagen: 'https://media.istockphoto.com/id/1480277406/es/foto/la-graduaci%C3%B3n-el-grupo-y-la-vista-trasera-de-los-estudiantes-celebran-el-%C3%A9xito-educativo.jpg?s=612x612&w=0&k=20&c=MX839Ue5i3yXjWLp6Ak-dSHn8OMlwqBLT34QfTKxSfM=',
      folioContrato: 'CTR-2026-0055'
    }
  ];

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
    return labels[estatus];
  }

  getEstatusClasses(estatus: EstatusEvento): string {
    const classes: Record<EstatusEvento, string> = {
      programado: 'bg-blue-100 text-blue-700',
      en_curso: 'bg-yellow-100 text-yellow-700',
      completado: 'bg-green-100 text-green-700',
      cancelado: 'bg-red-100 text-red-700',
      reprogramado: 'bg-purple-100 text-purple-700'
    };
    return classes[estatus];
  }

  formatearFecha(fecha: string): string {
    return new Date(fecha + 'T12:00:00').toLocaleDateString('es-MX', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
}
