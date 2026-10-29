import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarClient } from '@shared/components/navbar-client/navbar-client';
import { PagosService } from '@features/pagos/services/pagos.service';
import {
  PagoItem,
  EstadoCuentaEvento,
  ResumenPagos,
  EstatusPago,
  MetodoPago,
  RegistrarPagoRequest
} from '@core/models/pago.models';

@Component({
  selector: 'app-pagos',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarClient],
  templateUrl: './pagos.html'
})
export class Pagos implements OnInit {
  private readonly pagosService = inject(PagosService);
  private readonly cdr = inject(ChangeDetectorRef);

  busqueda = '';
  filtroEstatus: 'todos' | 'pendientes' | 'liquidados' = 'todos';
  cargando = true;
  clienteId: number | undefined = undefined;

  estadosCuenta: EstadoCuentaEvento[] = [];
  resumen: ResumenPagos = {
    totalPagado: 0,
    totalPendiente: 0,
    proximoPago: null,
    totalContratos: 0,
    conteoPagos: 0,
    conteoCuotasPendientes: 0
  };

  // Modal de Pago / Reportar Transferencia
  modalPagoAbierto = false;
  eventoSeleccionado: EstadoCuentaEvento | null = null;
  modoMonto: 'anticipo' | 'total' | 'personalizado' = 'anticipo';
  montoAPagar = 0;
  metodoSeleccionado: MetodoPago = 'transferencia';
  referenciaPago = '';
  notasPago = '';
  titularTarjeta = '';
  numeroTarjeta = '';
  vencimientoTarjeta = '';
  cvvTarjeta = '';
  procesandoPago = false;
  mensajeErrorPago: string | null = null;
  mensajeExitoPago: string | null = null;

  // Modal de Recibo / Ticket Digital
  modalReciboAbierto = false;
  pagoSeleccionadoRecibo: PagoItem | null = null;

  ngOnInit(): void {
    this.obtenerSesion();
    this.cargarDatos();
  }

  obtenerSesion(): void {
    const sessionStr = localStorage.getItem('session');
    if (sessionStr) {
      try {
        const session = JSON.parse(sessionStr);
        if (session?.cliente?.id) {
          this.clienteId = session.cliente.id;
        } else if (session?.clienteId) {
          this.clienteId = session.clienteId;
        }
      } catch (e) {
        console.error('Error al leer sesion:', e);
      }
    }
  }

  cargarDatos(): void {
    this.cargando = true;
    this.cdr.detectChanges();

    this.pagosService.getResumen(this.clienteId).subscribe({
      next: (res) => {
        this.resumen = res;
        this.cdr.detectChanges();
      }
    });

    this.pagosService.getEstadoCuenta(this.clienteId).subscribe({
      next: (lista) => {
        this.estadosCuenta = lista || [];
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar estado de cuenta:', err);
        this.estadosCuenta = [];
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  get eventosFiltrados(): EstadoCuentaEvento[] {
    const termino = this.busqueda.trim().toLowerCase();
    return this.estadosCuenta.filter(item => {
      const coincideBusqueda = !termino ||
        item.folioContrato.toLowerCase().includes(termino) ||
        item.evento.toLowerCase().includes(termino) ||
        item.pagos.some(p =>
          (p.ticketFolio && p.ticketFolio.toLowerCase().includes(termino)) ||
          (p.referencia && p.referencia.toLowerCase().includes(termino))
        );
      const coincideFiltro = this.filtroEstatus === 'todos' ||
        (this.filtroEstatus === 'pendientes' && item.saldoPendiente > 0.009) ||
        (this.filtroEstatus === 'liquidados' && item.saldoPendiente <= 0.009);
      return coincideBusqueda && coincideFiltro;
    });
  }

  get totalPagado(): number {
    return this.resumen.totalPagado;
  }

  get totalPendiente(): number {
    return this.resumen.totalPendiente;
  }

  get totalEventos(): number {
    return this.estadosCuenta.length;
  }

  // Flujo Modal de Pago por Evento
  abrirModalPago(evento: EstadoCuentaEvento): void {
    this.eventoSeleccionado = evento;
    if (evento.anticipoRestante > 0.009) {
      this.modoMonto = 'anticipo';
      this.montoAPagar = evento.anticipoRestante;
    } else {
      this.modoMonto = 'total';
      this.montoAPagar = evento.saldoPendiente;
    }

    this.metodoSeleccionado = 'transferencia';
    this.referenciaPago = '';
    this.notasPago = '';
    this.titularTarjeta = '';
    this.numeroTarjeta = '';
    this.vencimientoTarjeta = '';
    this.cvvTarjeta = '';
    this.mensajeErrorPago = null;
    this.mensajeExitoPago = null;
    this.procesandoPago = false;
    this.modalPagoAbierto = true;
    this.cdr.detectChanges();
  }

  seleccionarModoMonto(modo: 'anticipo' | 'total' | 'personalizado'): void {
    if (!this.eventoSeleccionado) return;
    this.modoMonto = modo;
    if (modo === 'anticipo') {
      this.montoAPagar = this.eventoSeleccionado.anticipoRestante;
    } else if (modo === 'total') {
      this.montoAPagar = this.eventoSeleccionado.saldoPendiente;
    }
    this.mensajeErrorPago = null;
    this.cdr.detectChanges();
  }

  ajustarMontoAlMaximo(): void {
    if (this.eventoSeleccionado) {
      this.montoAPagar = this.eventoSeleccionado.saldoPendiente;
      this.mensajeErrorPago = null;
      this.cdr.detectChanges();
    }
  }

  get esMontoExcedido(): boolean {
    return !!this.eventoSeleccionado && Number(this.montoAPagar) > this.eventoSeleccionado.saldoPendiente + 0.009;
  }

  get esMontoMenorAnticipo(): boolean {
    return !!this.eventoSeleccionado &&
      this.eventoSeleccionado.anticipoRestante > 0.009 &&
      Number(this.montoAPagar) + 0.009 < this.eventoSeleccionado.anticipoRestante;
  }

  cerrarModalPago(): void {
    this.modalPagoAbierto = false;
    this.eventoSeleccionado = null;
    this.mensajeErrorPago = null;
    this.mensajeExitoPago = null;
    this.cdr.detectChanges();
  }

  confirmarPago(): void {
    if (!this.eventoSeleccionado || this.procesandoPago) return;

    const monto = Number(this.montoAPagar);
    if (!monto || monto <= 0) {
      this.mensajeErrorPago = 'Por favor ingresa un monto válido a pagar.';
      return;
    }

    if (this.eventoSeleccionado.saldoPendiente <= 0.009) {
      this.mensajeErrorPago = 'Este evento ya se encuentra liquidado al 100%. No admite pagos adicionales.';
      return;
    }

    if (monto > this.eventoSeleccionado.saldoPendiente + 0.009) {
      this.mensajeErrorPago = `No es posible pagar más del 100% del valor del evento. El saldo pendiente máximo a liquidar es de $${this.eventoSeleccionado.saldoPendiente.toLocaleString('es-MX', { minimumFractionDigits: 2 })} MXN.`;
      return;
    }

    if (this.eventoSeleccionado.anticipoRestante > 0.01 && monto + 0.01 < this.eventoSeleccionado.anticipoRestante) {
      this.mensajeErrorPago = `El primer pago debe cubrir al menos el 50% de anticipo ($${this.eventoSeleccionado.anticipoRestante.toLocaleString('es-MX', { minimumFractionDigits: 2 })} MXN).`;
      return;
    }

    if (this.metodoSeleccionado === 'tarjeta_credito' || this.metodoSeleccionado === 'tarjeta_debito') {
      if (!this.titularTarjeta.trim() || !this.numeroTarjeta.trim() || !this.cvvTarjeta.trim()) {
        this.mensajeErrorPago = 'Por favor completa los datos de la tarjeta.';
        return;
      }
    }

    this.procesandoPago = true;
    this.mensajeErrorPago = null;

    const payload: RegistrarPagoRequest = {
      contratoId: this.eventoSeleccionado.contratoId,
      monto: monto,
      metodoPago: this.metodoSeleccionado,
      referencia: undefined, // Generado automáticamente por el backend
      notas: this.notasPago.trim() || undefined
    };

    this.pagosService.registrarPago(payload).subscribe({
      next: (res) => {
        this.procesandoPago = false;
        this.mensajeExitoPago = `Pago procesado con éxito. Se ha generado tu recibo con folio: ${res.ticketFolio || res.pagoId}`;
        this.cargarDatos();
        setTimeout(() => {
          this.cerrarModalPago();
        }, 1800);
      },
      error: (err) => {
        this.procesandoPago = false;
        console.error('Error al registrar pago:', err);
        this.mensajeErrorPago = err?.error?.error || 'No se pudo procesar el pago. Por favor intenta de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }

  // Flujo Modal de Recibo / Ticket Digital
  abrirModalRecibo(pago: PagoItem): void {
    this.pagoSeleccionadoRecibo = pago;
    this.modalReciboAbierto = true;
    this.cdr.detectChanges();
  }

  cerrarModalRecibo(): void {
    this.modalReciboAbierto = false;
    this.pagoSeleccionadoRecibo = null;
    this.cdr.detectChanges();
  }

  imprimirRecibo(): void {
    window.print();
  }

  getMetodoLabel(metodo: MetodoPago | string): string {
    const labels: Record<string, string> = {
      efectivo: 'Efectivo',
      transferencia: 'Transferencia SPEI',
      tarjeta_credito: 'Tarjeta de crédito',
      tarjeta_debito: 'Tarjeta de débito',
      cheque: 'Cheque',
      otro: 'Otro'
    };
    return labels[metodo] || metodo;
  }

  getEstatusPagoLabel(estatus: EstatusPago | string): string {
    const labels: Record<string, string> = {
      procesado: 'Procesado',
      pendiente: 'En validación',
      cancelado: 'Cancelado',
      reembolsado: 'Reembolsado'
    };
    return labels[estatus] || estatus;
  }

  getEstatusPagoClasses(estatus: EstatusPago | string): string {
    const classes: Record<string, string> = {
      procesado: 'bg-emerald-100 text-emerald-800 border-emerald-200',
      pendiente: 'bg-amber-100 text-amber-800 border-amber-200',
      cancelado: 'bg-rose-100 text-rose-800 border-rose-200',
      reembolsado: 'bg-gray-100 text-gray-800 border-gray-200'
    };
    return classes[estatus] || 'bg-gray-100 text-gray-800 border-gray-200';
  }

  getEstatusCuentaLabel(estatus: string, vencido: boolean): string {
    if (vencido) return 'Pago Vencido';
    switch (estatus) {
      case 'liquidado': return 'Liquidado (100%)';
      case 'anticipo_pagado': return 'Anticipo Cubierto (50%)';
      case 'pendiente_anticipo': return 'Pendiente Anticipo (50% mín.)';
      case 'cancelado': return 'Cancelado';
      default: return estatus;
    }
  }

  getEstatusCuentaClasses(estatus: string, vencido: boolean): string {
    if (vencido) return 'bg-rose-100 text-rose-800 border-rose-300';
    switch (estatus) {
      case 'liquidado': return 'bg-emerald-100 text-emerald-800 border-emerald-300';
      case 'anticipo_pagado': return 'bg-blue-100 text-blue-800 border-blue-300';
      case 'pendiente_anticipo': return 'bg-amber-100 text-amber-800 border-amber-300';
      case 'cancelado': return 'bg-gray-100 text-gray-800 border-gray-300';
      default: return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  }

  formatearFecha(fecha?: string | null): string {
    if (!fecha) return 'Por definir';
    return new Date(fecha + 'T12:00:00').toLocaleDateString('es-MX', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
