import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarClient } from '../../components/navbar-client/navbar-client';

export type EstatusCotizacion = 'borrador' | 'enviada' | 'aceptada' | 'rechazada' | 'vencida';

export interface CotizacionItem {
  descripcion: string;
  monto: number;
}

export interface Cotizacion {
  id: number;
  folio: string;
  evento: string;
  fechaEvento: string;
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

@Component({
  selector: 'app-cotizaciones',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarClient],
  templateUrl: './cotizaciones.html'
})
export class Cotizaciones {
  busqueda = '';
  filtroEstatus: EstatusCotizacion | 'todos' = 'todos';
  cotizacionSeleccionada: Cotizacion | null = null;

  readonly cotizaciones: Cotizacion[] = [
    {
      id: 1,
      folio: 'COT-2026-0089',
      evento: 'Boda premium',
      fechaEvento: '2026-08-15',
      invitados: 280,
      total: 135000,
      descuento: 5000,
      totalFinal: 130000,
      estatus: 'aceptada',
      fechaVigencia: '2026-04-30',
      fechaCreacion: '2026-03-15',
      notas: 'Incluye banquete premium y DJ en vivo.',
      items: [
        { descripcion: 'Paquete Boda premium', monto: 60000 },
        { descripcion: 'Banquete (280 invitados × $250)', monto: 70000 },
        { descripcion: 'Música y DJ en vivo', monto: 5000 }
      ]
    },
    {
      id: 2,
      folio: 'COT-2026-0102',
      evento: 'Graduación',
      fechaEvento: '2026-06-28',
      invitados: 120,
      total: 45000,
      descuento: 0,
      totalFinal: 45000,
      estatus: 'enviada',
      fechaVigencia: '2026-05-15',
      fechaCreacion: '2026-04-20',
      notas: 'Solicitan 2 horas adicionales de evento.',
      items: [
        { descripcion: 'Paquete Graduación', monto: 15000 },
        { descripcion: 'Horas adicionales (2 hrs)', monto: 2000 },
        { descripcion: 'Banquete (120 invitados × $250)', monto: 30000 }
      ]
    },
    {
      id: 3,
      folio: 'COT-2026-0075',
      evento: 'Evento corporativo',
      fechaEvento: '2026-07-10',
      invitados: 80,
      total: 15000,
      descuento: 0,
      totalFinal: 15000,
      estatus: 'aceptada',
      fechaVigencia: '2026-04-01',
      fechaCreacion: '2026-03-01',
      notas: 'Coffee break incluido para 80 asistentes.',
      items: [
        { descripcion: 'Paquete Evento corporativo', monto: 12000 },
        { descripcion: 'Equipo audiovisual adicional', monto: 3000 }
      ]
    },
    {
      id: 4,
      folio: 'COT-2025-0441',
      evento: 'Boda estándar',
      fechaEvento: '2025-12-10',
      invitados: 100,
      total: 52000,
      descuento: 2000,
      totalFinal: 50000,
      estatus: 'vencida',
      fechaVigencia: '2025-11-01',
      fechaCreacion: '2025-10-05',
      notas: 'Cotización no confirmada a tiempo.',
      items: [
        { descripcion: 'Paquete Boda estándar', monto: 25000 },
        { descripcion: 'Decoración temática', monto: 3000 },
        { descripcion: 'Banquete (100 invitados × $250)', monto: 25000 }
      ]
    },
    {
      id: 5,
      folio: 'COT-2026-0118',
      evento: 'XV años',
      fechaEvento: '2026-09-20',
      invitados: 150,
      total: 55500,
      descuento: 0,
      totalFinal: 55500,
      estatus: 'borrador',
      fechaVigencia: '2026-06-30',
      fechaCreacion: '2026-05-28',
      notas: 'Pendiente de revisión final por el cliente.',
      items: [
        { descripcion: 'Paquete XV años', monto: 18000 },
        { descripcion: 'Decoración premium', monto: 3000 },
        { descripcion: 'Banquete (150 invitados × $250)', monto: 37500 }
      ]
    }
  ];

  get cotizacionesFiltradas(): Cotizacion[] {
    const termino = this.busqueda.trim().toLowerCase();
    return this.cotizaciones.filter(cot => {
      const coincideBusqueda = !termino ||
        cot.folio.toLowerCase().includes(termino) ||
        cot.evento.toLowerCase().includes(termino);
      const coincideEstatus = this.filtroEstatus === 'todos' || cot.estatus === this.filtroEstatus;
      return coincideBusqueda && coincideEstatus;
    });
  }

  get totalAceptadas(): number {
    return this.cotizaciones.filter(c => c.estatus === 'aceptada').length;
  }

  get totalPendientes(): number {
    return this.cotizaciones.filter(c => c.estatus === 'enviada' || c.estatus === 'borrador').length;
  }

  get montoTotalAceptado(): number {
    return this.cotizaciones
      .filter(c => c.estatus === 'aceptada')
      .reduce((sum, c) => sum + c.totalFinal, 0);
  }

  abrirDetalle(cotizacion: Cotizacion): void {
    this.cotizacionSeleccionada = cotizacion;
  }

  cerrarDetalle(): void {
    this.cotizacionSeleccionada = null;
  }

  getEstatusLabel(estatus: EstatusCotizacion): string {
    const labels: Record<EstatusCotizacion, string> = {
      borrador: 'Borrador',
      enviada: 'Enviada',
      aceptada: 'Aceptada',
      rechazada: 'Rechazada',
      vencida: 'Vencida'
    };
    return labels[estatus];
  }

  getEstatusClasses(estatus: EstatusCotizacion): string {
    const classes: Record<EstatusCotizacion, string> = {
      borrador: 'bg-gray-100 text-gray-700',
      enviada: 'bg-blue-100 text-blue-700',
      aceptada: 'bg-green-100 text-green-700',
      rechazada: 'bg-red-100 text-red-700',
      vencida: 'bg-orange-100 text-orange-700'
    };
    return classes[estatus];
  }

  formatearFecha(fecha: string): string {
    return new Date(fecha + 'T12:00:00').toLocaleDateString('es-MX', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
