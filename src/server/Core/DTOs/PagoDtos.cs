using System;
using System.Collections.Generic;

namespace Server.Core.DTOs;

public record PagoItemDto(
    int Id,
    int ContratoId,
    string FolioContrato,
    string Evento,
    int? PlanPagoId,
    int NumeroPago,
    decimal Monto,
    string MetodoPago,
    string TipoTransaccion,
    string? Referencia,
    string FechaPago,
    string Estatus,
    string? Notas,
    string? TicketFolio,
    string ClienteNombre
);

public record CuotaPendienteDto(
    int Id,
    int ContratoId,
    string FolioContrato,
    string Evento,
    int NumeroPago,
    decimal Monto,
    string FechaVencimiento,
    string Estatus,
    string ClienteNombre
);

public record ResumenPagosDto(
    decimal TotalPagado,
    decimal TotalPendiente,
    CuotaPendienteDto? ProximoPago,
    int TotalContratos,
    int ConteoPagos,
    int ConteoCuotasPendientes
);

/// <summary>
/// Estado de cuenta consolidado de un evento contratado (un registro por contrato).
/// Agrupa todos los abonos realizados y el saldo restante en una sola vista.
/// Estatus: pendiente_anticipo | anticipo_pagado | liquidado | cancelado.
/// </summary>
public record EstadoCuentaEventoDto(
    int ContratoId,
    string FolioContrato,
    string Evento,
    string? Categoria,
    string? ImagenUrl,
    string? FechaEvento,
    int ClienteId,
    string ClienteNombre,
    decimal TotalContrato,
    decimal TotalPagado,
    decimal SaldoPendiente,
    decimal AnticipoMinimo,
    decimal AnticipoRestante,
    decimal PorcentajePagado,
    string Estatus,
    bool Vencido,
    string? FechaLimiteAnticipo,
    string? FechaLimiteLiquidacion,
    List<PagoItemDto> Pagos
);

public record RegistrarPagoRequest(
    int ContratoId,
    int? PlanPagoId,
    decimal Monto,
    string MetodoPago,
    string? TipoTransaccion,
    string? Referencia,
    string? FechaPago,
    string? Notas
);

public record RegistrarPagoResponse(
    int PagoId,
    int? TicketId,
    string? TicketFolio,
    string FolioContrato,
    decimal Monto,
    string EstatusCuota,
    string Mensaje
);
