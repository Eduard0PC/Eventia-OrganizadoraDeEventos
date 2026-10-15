import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarClient } from '@shared/components/navbar-client/navbar-client';
import { ToastComponent } from '@shared/components/toast/toast.component';
import { ToastService } from '@shared/components/toast/toast.service';
import { Cotizacion, EstatusCotizacion, ServicioExtraItem } from '@core/models/cotizacion.models';
import { CotizacionesService } from '@features/cotizaciones/services/cotizaciones.service';

@Component({
  selector: 'app-cotizaciones',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarClient, ToastComponent],
  templateUrl: './cotizaciones.html'
})
export class Cotizaciones implements OnInit {
  private readonly cotizacionesService = inject(CotizacionesService);
  private readonly toastService = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  busqueda = '';
  filtroEstatus: EstatusCotizacion | 'todos' = 'todos';
  cotizacionSeleccionada: Cotizacion | null = null;
  cotizacionEditando: Cotizacion | null = null;

  editFechaEvento = '';
  editInvitados = 50;
  editHorasAdicionales = 0;
  editNotas = '';
  guardandoEdicion = false;
  invitadosPresets = [50, 100, 150, 200];

  serviciosExtraEdicion: ServicioExtraItem[] = [];
  cargandoServiciosExtraEdicion = false;

  cotizaciones: Cotizacion[] = [];
  cargando = true;

  ngOnInit(): void {
    this.cargarCotizaciones();
  }

  cargarCotizaciones(): void {
    this.cargando = true;
    this.cdr.detectChanges();

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
        console.error('Error reading session from localStorage:', e);
      }
    }

    this.cotizacionesService.getCotizaciones(clienteId).subscribe({
      next: (data) => {
        this.cotizaciones = data || [];
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar cotizaciones:', err);
        this.cotizaciones = [];
        this.cargando = false;
        this.cdr.detectChanges();
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

  abrirEditarBorrador(cotizacion: Cotizacion): void {
    if (cotizacion.estatus !== 'borrador') {
      this.toastService.showError('Las cotizaciones enviadas solo pueden ser consultadas.');
      return;
    }
    this.cotizacionEditando = cotizacion;
    this.editFechaEvento = cotizacion.fechaEvento || '';
    this.editInvitados = cotizacion.invitados || 50;

    const itemHoras = cotizacion.items.find(i => i.descripcion.toLowerCase().includes('horas adicionales'));
    this.editHorasAdicionales = itemHoras ? (itemHoras.cantidad || 0) : 0;
    this.editNotas = cotizacion.notas || '';

    this.serviciosExtraEdicion = [];
    this.cargarServiciosExtraEdicion(cotizacion);
  }

  cerrarEdicion(): void {
    this.cotizacionEditando = null;
    this.serviciosExtraEdicion = [];
  }

  cargarServiciosExtraEdicion(cotizacion: Cotizacion): void {
    this.cargandoServiciosExtraEdicion = true;
    this.cdr.detectChanges();

    this.cotizacionesService.getServiciosExtra(cotizacion.categoriaEvento).subscribe({
      next: (servicios) => {
        const itemsCot = cotizacion.items || [];
        this.serviciosExtraEdicion = (servicios || []).map(s => {
          const yaSeleccionado = itemsCot.some(i =>
            (i.esServicioExtra && i.catalogoServicioId === s.id) ||
            i.catalogoServicioId === s.id ||
            (i.tipo === 'servicio' && i.descripcion?.toLowerCase() === s.nombre.toLowerCase())
          );
          return {
            ...s,
            seleccionado: yaSeleccionado
          };
        });
        this.cargandoServiciosExtraEdicion = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar servicios extra en edición:', err);
        this.serviciosExtraEdicion = [];
        this.cargandoServiciosExtraEdicion = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleServicioExtraEdicion(servicio: ServicioExtraItem): void {
    servicio.seleccionado = !servicio.seleccionado;
  }

  getServiciosExtraSeleccionadosEdicion(): ServicioExtraItem[] {
    return this.serviciosExtraEdicion.filter(s => s.seleccionado);
  }

  getCostoServiciosExtraEdicion(): number {
    return this.serviciosExtraEdicion
      .filter(s => s.seleccionado)
      .reduce((sum, s) => sum + Number(s.precioBase || 0), 0);
  }

  selectEditInvitadosPreset(cantidad: number): void {
    this.editInvitados = cantidad;
  }

  incrementarEditHoras(): void {
    if (this.editHorasAdicionales < 8) {
      this.editHorasAdicionales++;
    }
  }

  decrementarEditHoras(): void {
    if (this.editHorasAdicionales > 0) {
      this.editHorasAdicionales--;
    }
  }

  getCapacidadBaseEdicion(): number {
    return 50;
  }

  getInvitadosAdicionalesEdicion(): number {
    return Math.max(0, (this.editInvitados || 0) - this.getCapacidadBaseEdicion());
  }

  getCostoExtraInvitadosEdicion(): number {
    return this.getInvitadosAdicionalesEdicion() * 150;
  }

  getCostoHorasExtraEdicion(): number {
    return (this.editHorasAdicionales || 0) * 1000;
  }

  getPrecioBaseEdicion(): number {
    if (!this.cotizacionEditando) return 0;
    const baseItem = this.cotizacionEditando.items.find(i => i.tipo === 'evento');
    return baseItem ? baseItem.precioUnitario || 0 : this.cotizacionEditando.total;
  }

  getEstimatedTotalEdicion(): number {
    if (!this.cotizacionEditando) return 0;
    return this.getPrecioBaseEdicion() +
      this.getCostoExtraInvitadosEdicion() +
      this.getCostoHorasExtraEdicion() +
      this.getCostoServiciosExtraEdicion();
  }

  guardarEdicion(estatus: 'borrador' | 'enviada'): void {
    if (!this.cotizacionEditando || !this.editFechaEvento || !this.editInvitados || this.guardandoEdicion) {
      return;
    }

    let clienteId: number | undefined = undefined;
    const sessionStr = localStorage.getItem('session');
    if (sessionStr) {
      try {
        const session = JSON.parse(sessionStr);
        if (session?.cliente?.id) {
          clienteId = session.cliente.id;
        }
      } catch (e) {
        console.error('Error parsing session:', e);
      }
    }

    const catalogoId = this.cotizacionEditando.catalogoEventoId || 1;
    const serviciosExtraIds = this.serviciosExtraEdicion
      .filter(s => s.seleccionado)
      .map(s => s.id);

    this.guardandoEdicion = true;

    this.cotizacionesService.actualizarCotizacion(this.cotizacionEditando.id, {
      clienteId: clienteId,
      catalogoEventoId: catalogoId,
      fechaEvento: this.editFechaEvento,
      invitados: this.editInvitados,
      horasAdicionales: this.editHorasAdicionales,
      notas: this.editNotas,
      estatus: estatus,
      serviciosExtraIds: serviciosExtraIds
    }).subscribe({
      next: (response) => {
        this.guardandoEdicion = false;
        this.cerrarEdicion();
        this.cargarCotizaciones();
        if (estatus === 'borrador') {
          this.toastService.showSuccess('Borrador actualizado exitosamente');
        } else {
          this.toastService.showSuccess('Cotización enviada a revisión exitosamente');
        }
      },
      error: (err) => {
        this.guardandoEdicion = false;
        console.error('Error al actualizar cotización:', err);
        this.toastService.showError('Error al actualizar la cotización. Reintenta.');
      }
    });
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
      borrador: 'bg-gray-100 text-gray-700 border border-gray-300',
      enviada: 'bg-blue-100 text-blue-800 border border-blue-200 font-bold',
      aceptada: 'bg-green-100 text-green-700 border border-green-200',
      rechazada: 'bg-red-100 text-red-700 border border-red-200',
      vencida: 'bg-orange-100 text-orange-700 border border-orange-200'
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
