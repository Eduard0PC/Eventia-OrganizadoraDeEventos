export type EstatusPago = 'procesado' | 'pendiente' | 'cancelado' | 'reembolsado';
export type EstatusPlanPago = 'pendiente' | 'pagado' | 'vencido' | 'cancelado';
export type MetodoPago = 'efectivo' | 'transferencia' | 'tarjeta_credito' | 'tarjeta_debito' | 'cheque' | 'otro';
export type TipoTransaccion = 'anticipo' | 'parcial' | 'liquidacion' | 'devolucion';

export interface PagoItem {
  id: number;
  contratoId: number;
  folioContrato: string;
  evento: string;
  planPagoId?: number | null;
  numeroPago: number;
  monto: number;
  metodoPago: MetodoPago;
  tipoTransaccion: TipoTransaccion;
  referencia?: string | null;
  fechaPago: string;
  estatus: EstatusPago;
  notas?: string | null;
  ticketFolio?: string | null;
  clienteNombre: string;
}

export interface CuotaPendiente {
  id: number;
  contratoId: number;
  folioContrato: string;
  evento: string;
  numeroPago: number;
  monto: number;
  fechaVencimiento: string;
  estatus: EstatusPlanPago;
  clienteNombre: string;
}

export interface ResumenPagos {
  totalPagado: number;
  totalPendiente: number;
  proximoPago: CuotaPendiente | null;
  totalContratos: number;
  conteoPagos: number;
  conteoCuotasPendientes: number;
}

export type EstatusEstadoCuenta = 'pendiente_anticipo' | 'anticipo_pagado' | 'liquidado' | 'cancelado';

export interface EstadoCuentaEvento {
  contratoId: number;
  folioContrato: string;
  evento: string;
  categoria?: string | null;
  imagenUrl?: string | null;
  fechaEvento?: string | null;
  clienteId: number;
  clienteNombre: string;
  totalContrato: number;
  totalPagado: number;
  saldoPendiente: number;
  anticipoMinimo: number;
  anticipoRestante: number;
  porcentajePagado: number;
  estatus: EstatusEstadoCuenta;
  vencido: boolean;
  fechaLimiteAnticipo?: string | null;
  fechaLimiteLiquidacion?: string | null;
  pagos: PagoItem[];
}

export interface RegistrarPagoRequest {
  contratoId: number;
  planPagoId?: number | null;
  monto: number;
  metodoPago: string;
  tipoTransaccion?: string;
  referencia?: string;
  fechaPago?: string;
  notas?: string;
}

export interface RegistrarPagoResponse {
  pagoId: number;
  ticketId?: number;
  ticketFolio?: string;
  folioContrato: string;
  monto: number;
  estatusCuota: string;
  mensaje: string;
}

