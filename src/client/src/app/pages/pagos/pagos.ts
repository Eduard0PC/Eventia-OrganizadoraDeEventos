import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarClient } from '../../components/navbar-client/navbar-client';

export type EstatusPago = 'procesado' | 'pendiente' | 'cancelado' | 'reembolsado';
export type EstatusPlanPago = 'pendiente' | 'pagado' | 'vencido' | 'cancelado';
export type MetodoPago = 'efectivo' | 'transferencia' | 'tarjeta_credito' | 'tarjeta_debito' | 'cheque' | 'otro';

export interface Pago {
  id: number;
  folioContrato: string;
  evento: string;
  monto: number;
  metodoPago: MetodoPago;
  fechaPago: string;
  estatus: EstatusPago;
  referencia: string;
  numeroPago: number;
}

export interface CuotaPendiente {
  id: number;
  folioContrato: string;
  evento: string;
  numeroPago: number;
  monto: number;
  fechaVencimiento: string;
  estatus: EstatusPlanPago;
}

@Component({
  selector: 'app-pagos',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarClient],
  templateUrl: './pagos.html'
})
export class Pagos {
  busqueda = '';
  filtroEstatus: EstatusPago | 'todos' = 'todos';
  tabActiva: 'historial' | 'pendientes' = 'historial';

  readonly pagos: Pago[] = [
    {
      id: 1,
      folioContrato: 'CTR-2026-0042',
      evento: 'Boda premium',
      monto: 39000,
      metodoPago: 'transferencia',
      fechaPago: '2026-03-20',
      estatus: 'procesado',
      referencia: 'TRF-8847291',
      numeroPago: 1
    },
    {
      id: 2,
      folioContrato: 'CTR-2026-0042',
      evento: 'Boda premium',
      monto: 39000,
      metodoPago: 'transferencia',
      fechaPago: '2026-04-20',
      estatus: 'procesado',
      referencia: 'TRF-9103456',
      numeroPago: 2
    },
    {
      id: 3,
      folioContrato: 'CTR-2025-0187',
      evento: 'XV años',
      monto: 18500,
      metodoPago: 'tarjeta_credito',
      fechaPago: '2025-04-10',
      estatus: 'procesado',
      referencia: 'TC-5521-8890',
      numeroPago: 1
    },
    {
      id: 4,
      folioContrato: 'CTR-2025-0187',
      evento: 'XV años',
      monto: 18500,
      metodoPago: 'tarjeta_credito',
      fechaPago: '2025-05-10',
      estatus: 'procesado',
      referencia: 'TC-5521-9012',
      numeroPago: 2
    },
    {
      id: 5,
      folioContrato: 'CTR-2026-0031',
      evento: 'Evento corporativo',
      monto: 7500,
      metodoPago: 'transferencia',
      fechaPago: '2026-03-05',
      estatus: 'procesado',
      referencia: 'TRF-7701234',
      numeroPago: 1
    },
    {
      id: 6,
      folioContrato: 'CTR-2026-0055',
      evento: 'Graduación',
      monto: 15000,
      metodoPago: 'efectivo',
      fechaPago: '2026-05-01',
      estatus: 'procesado',
      referencia: 'EFE-20260501',
      numeroPago: 1
    }
  ];

  readonly cuotasPendientes: CuotaPendiente[] = [
    {
      id: 1,
      folioContrato: 'CTR-2026-0042',
      evento: 'Boda premium',
      numeroPago: 3,
      monto: 39000,
      fechaVencimiento: '2026-05-20',
      estatus: 'pendiente'
    },
    {
      id: 2,
      folioContrato: 'CTR-2026-0042',
      evento: 'Boda premium',
      numeroPago: 4,
      monto: 13000,
      fechaVencimiento: '2026-06-20',
      estatus: 'pendiente'
    },
    {
      id: 3,
      folioContrato: 'CTR-2026-0031',
      evento: 'Evento corporativo',
      numeroPago: 2,
      monto: 7500,
      fechaVencimiento: '2026-04-05',
      estatus: 'vencido'
    },
    {
      id: 4,
      folioContrato: 'CTR-2026-0055',
      evento: 'Graduación',
      numeroPago: 2,
      monto: 15000,
      fechaVencimiento: '2026-06-01',
      estatus: 'pendiente'
    },
    {
      id: 5,
      folioContrato: 'CTR-2026-0055',
      evento: 'Graduación',
      numeroPago: 3,
      monto: 15000,
      fechaVencimiento: '2026-07-01',
      estatus: 'pendiente'
    }
  ];

  get pagosFiltrados(): Pago[] {
    const termino = this.busqueda.trim().toLowerCase();
    return this.pagos.filter(pago => {
      const coincideBusqueda = !termino ||
        pago.folioContrato.toLowerCase().includes(termino) ||
        pago.evento.toLowerCase().includes(termino) ||
        pago.referencia.toLowerCase().includes(termino);
      const coincideEstatus = this.filtroEstatus === 'todos' || pago.estatus === this.filtroEstatus;
      return coincideBusqueda && coincideEstatus;
    });
  }

  get totalPagado(): number {
    return this.pagos
      .filter(p => p.estatus === 'procesado')
      .reduce((sum, p) => sum + p.monto, 0);
  }

  get totalPendiente(): number {
    return this.cuotasPendientes
      .filter(c => c.estatus === 'pendiente' || c.estatus === 'vencido')
      .reduce((sum, c) => sum + c.monto, 0);
  }

  get proximoPago(): CuotaPendiente | null {
    const pendientes = this.cuotasPendientes
      .filter(c => c.estatus === 'pendiente' || c.estatus === 'vencido')
      .sort((a, b) => a.fechaVencimiento.localeCompare(b.fechaVencimiento));
    return pendientes[0] ?? null;
  }

  getMetodoLabel(metodo: MetodoPago): string {
    const labels: Record<MetodoPago, string> = {
      efectivo: 'Efectivo',
      transferencia: 'Transferencia',
      tarjeta_credito: 'Tarjeta de crédito',
      tarjeta_debito: 'Tarjeta de débito',
      cheque: 'Cheque',
      otro: 'Otro'
    };
    return labels[metodo];
  }

  getEstatusPagoLabel(estatus: EstatusPago): string {
    const labels: Record<EstatusPago, string> = {
      procesado: 'Procesado',
      pendiente: 'Pendiente',
      cancelado: 'Cancelado',
      reembolsado: 'Reembolsado'
    };
    return labels[estatus];
  }

  getEstatusPagoClasses(estatus: EstatusPago): string {
    const classes: Record<EstatusPago, string> = {
      procesado: 'bg-green-100 text-green-700',
      pendiente: 'bg-yellow-100 text-yellow-700',
      cancelado: 'bg-red-100 text-red-700',
      reembolsado: 'bg-gray-100 text-gray-700'
    };
    return classes[estatus];
  }

  getEstatusCuotaLabel(estatus: EstatusPlanPago): string {
    const labels: Record<EstatusPlanPago, string> = {
      pendiente: 'Pendiente',
      pagado: 'Pagado',
      vencido: 'Vencido',
      cancelado: 'Cancelado'
    };
    return labels[estatus];
  }

  getEstatusCuotaClasses(estatus: EstatusPlanPago): string {
    const classes: Record<EstatusPlanPago, string> = {
      pendiente: 'bg-yellow-100 text-yellow-700',
      pagado: 'bg-green-100 text-green-700',
      vencido: 'bg-red-100 text-red-700',
      cancelado: 'bg-gray-100 text-gray-700'
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
