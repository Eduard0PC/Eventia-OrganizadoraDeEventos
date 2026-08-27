import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarClient } from '@shared/components/navbar-client/navbar-client';
import { Cotizacion, EstatusCotizacion } from '@core/models/cotizacion.models';
import { CotizacionesService } from '@features/cotizaciones/services/cotizaciones.service';

@Component({
  selector: 'app-cotizaciones',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarClient],
  templateUrl: './cotizaciones.html'
})
export class Cotizaciones implements OnInit {
  private readonly cotizacionesService = inject(CotizacionesService);

  busqueda = '';
  filtroEstatus: EstatusCotizacion | 'todos' = 'todos';
  cotizacionSeleccionada: Cotizacion | null = null;
  cotizaciones: Cotizacion[] = [];
  cargando = true;

  ngOnInit(): void {
    this.cargarCotizaciones();
  }

  cargarCotizaciones(): void {
    this.cargando = true;
    let clienteId: number | undefined = undefined;
    const sessionStr = localStorage.getItem('session');
    if (sessionStr) {
      try {
        const session = JSON.parse(sessionStr);
        if (session?.cliente?.id) {
          clienteId = session.cliente.id;
        }
      } catch (e) {
        console.error('Error reading session from localStorage:', e);
      }
    }

    this.cotizacionesService.getCotizaciones(clienteId).subscribe({
      next: (data) => {
        this.cotizaciones = data;
        this.cargando = false;
      },
      error: (err) => {
        console.error('Error al cargar cotizaciones:', err);
        this.cargando = false;
      }
    });
  }

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
      .reduce((sum, c) => sum + Number(c.totalFinal || c.total), 0);
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
    return labels[estatus] || estatus;
  }

  getEstatusClasses(estatus: EstatusCotizacion): string {
    const classes: Record<EstatusCotizacion, string> = {
      borrador: 'bg-gray-100 text-gray-700',
      enviada: 'bg-blue-100 text-blue-700',
      aceptada: 'bg-green-100 text-green-700',
      rechazada: 'bg-red-100 text-red-700',
      vencida: 'bg-orange-100 text-orange-700'
    };
    return classes[estatus] || 'bg-gray-100 text-gray-700';
  }

  formatearFecha(fecha?: string): string {
    if (!fecha) return 'Sin fecha';
    return new Date(fecha + 'T12:00:00').toLocaleDateString('es-MX', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
