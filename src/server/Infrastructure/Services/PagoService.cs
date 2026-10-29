using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Core.DTOs;
using Server.Core.Entities;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Infrastructure.Services;

public class PagoService : IPagoService
{
    private readonly AppDbContext _db;

    public PagoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PagoItemDto>> GetPagosAsync(int? clienteId, int? contratoId, CancellationToken cancellationToken = default)
    {
        var query = _db.Pagos
            .AsNoTracking()
            .Include(p => p.Contrato)
                .ThenInclude(c => c.Cliente)
            .Include(p => p.Contrato)
                .ThenInclude(c => c.EventosContratados)
                    .ThenInclude(ev => ev.CatalogoEvento)
            .Include(p => p.PlanPago)
            .Include(p => p.Ticket)
            .AsQueryable();

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.Contrato.ClienteId == clienteId.Value);
        }

        if (contratoId.HasValue)
        {
            query = query.Where(p => p.ContratoId == contratoId.Value);
        }

        var list = await query
            .OrderByDescending(p => p.FechaPago)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        return list.Select(p =>
        {
            var ev = p.Contrato.EventosContratados.FirstOrDefault();
            string nombreEvento = ev?.CatalogoEvento?.Nombre ?? "Evento Contratado";
            string clienteNombre = p.Contrato.Cliente != null
                ? $"{p.Contrato.Cliente.Nombre} {p.Contrato.Cliente.Apellido}".Trim()
                : "";

            return new PagoItemDto(
                p.Id,
                p.ContratoId,
                p.Contrato.Folio,
                nombreEvento,
                p.PlanPagoId,
                p.PlanPago?.NumeroPago ?? 1,
                p.Monto,
                p.MetodoPago,
                p.TipoTransaccion,
                p.Referencia,
                p.FechaPago.ToString("yyyy-MM-dd"),
                p.Estatus,
                p.Notas,
                p.Ticket?.Folio,
                clienteNombre
            );
        }).ToList();
    }

    public async Task<List<CuotaPendienteDto>> GetCuotasPendientesAsync(int? clienteId, int? contratoId, CancellationToken cancellationToken = default)
    {
        // First, check if contracts have plan_pagos. If not, auto-generate them.
        if (contratoId.HasValue)
        {
            await GenerarPlanPagosDefaultAsync(contratoId.Value, cancellationToken);
        }
        else if (clienteId.HasValue)
        {
            var contratosCliente = await _db.Contratos
                .Where(c => c.ClienteId == clienteId.Value && c.Estatus != "cancelado")
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            foreach (var cid in contratosCliente)
            {
                await GenerarPlanPagosDefaultAsync(cid, cancellationToken);
            }
        }

        var query = _db.PlanPagos
            .AsNoTracking()
            .Include(pp => pp.Contrato)
                .ThenInclude(c => c.Cliente)
            .Include(pp => pp.Contrato)
                .ThenInclude(c => c.EventosContratados)
                    .ThenInclude(ev => ev.CatalogoEvento)
            .Where(pp => pp.Estatus != "pagado" && pp.Estatus != "cancelado" && pp.Contrato.Estatus != "cancelado")
            .AsQueryable();

        if (clienteId.HasValue)
        {
            query = query.Where(pp => pp.Contrato.ClienteId == clienteId.Value);
        }

        if (contratoId.HasValue)
        {
            query = query.Where(pp => pp.ContratoId == contratoId.Value);
        }

        var list = await query
            .OrderBy(pp => pp.FechaVencimiento)
            .ThenBy(pp => pp.Id)
            .ToListAsync(cancellationToken);

        var hoy = DateTime.UtcNow.Date;

        return list.Select(pp =>
        {
            var ev = pp.Contrato.EventosContratados.FirstOrDefault();
            string nombreEvento = ev?.CatalogoEvento?.Nombre ?? "Evento Contratado";
            string clienteNombre = pp.Contrato.Cliente != null
                ? $"{pp.Contrato.Cliente.Nombre} {pp.Contrato.Cliente.Apellido}".Trim()
                : "";

            string estatus = pp.Estatus;
            if (estatus == "pendiente" && pp.FechaVencimiento.Date < hoy)
            {
                estatus = "vencido";
            }

            return new CuotaPendienteDto(
                pp.Id,
                pp.ContratoId,
                pp.Contrato.Folio,
                nombreEvento,
                pp.NumeroPago,
                pp.Monto,
                pp.FechaVencimiento.ToString("yyyy-MM-dd"),
                estatus,
                clienteNombre
            );
        }).ToList();
    }

    public async Task<ResumenPagosDto> GetResumenAsync(int? clienteId, CancellationToken cancellationToken = default)
    {
        var pagos = await GetPagosAsync(clienteId, null, cancellationToken);
        var cuotas = await GetCuotasPendientesAsync(clienteId, null, cancellationToken);

        decimal totalPagado = pagos
            .Where(p => p.Estatus == "procesado")
            .Sum(p => p.Monto);

        decimal totalPendiente = cuotas
            .Where(c => c.Estatus == "pendiente" || c.Estatus == "vencido")
            .Sum(c => c.Monto);

        var proximo = cuotas
            .Where(c => c.Estatus == "pendiente" || c.Estatus == "vencido")
            .OrderBy(c => c.FechaVencimiento)
            .FirstOrDefault();

        int totalContratos = clienteId.HasValue
            ? await _db.Contratos.CountAsync(c => c.ClienteId == clienteId.Value, cancellationToken)
            : await _db.Contratos.CountAsync(cancellationToken);

        return new ResumenPagosDto(
            totalPagado,
            totalPendiente,
            proximo,
            totalContratos,
            pagos.Count,
            cuotas.Count
        );
    }

    public async Task<List<EstadoCuentaEventoDto>> GetEstadoCuentaAsync(int? clienteId, int? contratoId, CancellationToken cancellationToken = default)
    {
        // Asegura que todos los contratos consultados tengan su plan de pagos (anticipo + liquidación)
        var contratosSinPlan = await _db.Contratos
            .Where(c => (!clienteId.HasValue || c.ClienteId == clienteId.Value)
                     && (!contratoId.HasValue || c.Id == contratoId.Value)
                     && !c.PlanPagos.Any())
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var cid in contratosSinPlan)
        {
            await GenerarPlanPagosDefaultAsync(cid, cancellationToken);
        }

        var query = _db.Contratos
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.EventosContratados)
                .ThenInclude(ev => ev.CatalogoEvento)
            .Include(c => c.PlanPagos)
            .Include(c => c.Pagos)
                .ThenInclude(p => p.Ticket)
            .Include(c => c.Pagos)
                .ThenInclude(p => p.PlanPago)
            .AsQueryable();

        if (clienteId.HasValue)
        {
            query = query.Where(c => c.ClienteId == clienteId.Value);
        }

        if (contratoId.HasValue)
        {
            query = query.Where(c => c.Id == contratoId.Value);
        }

        var contratos = await query
            .AsSplitQuery()
            .OrderBy(c => c.FechaInicio)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        var hoy = DateTime.UtcNow.Date;

        return contratos.Select(c =>
        {
            var ev = c.EventosContratados.FirstOrDefault();
            string nombreEvento = ev?.CatalogoEvento?.Nombre ?? "Evento Contratado";
            string clienteNombre = c.Cliente != null
                ? $"{c.Cliente.Nombre} {c.Cliente.Apellido}".Trim()
                : "";

            var planes = c.PlanPagos.OrderBy(pp => pp.NumeroPago).ToList();
            var planAnticipo = planes.FirstOrDefault();
            var planLiquidacion = planes.Count > 1 ? planes.Last() : null;

            decimal total = c.TotalContrato;
            decimal pagado = c.Pagos.Where(p => p.Estatus == "procesado").Sum(p => p.Monto);
            decimal saldo = Math.Max(0m, total - pagado);
            decimal anticipoMinimo = ObtenerAnticipoMinimo(c);
            decimal anticipoRestante = Math.Max(0m, anticipoMinimo - pagado);
            decimal porcentaje = total > 0 ? Math.Min(100m, Math.Round(pagado * 100m / total, 2)) : 0m;

            string estatus;
            bool vencido = false;
            if (c.Estatus == "cancelado")
            {
                estatus = "cancelado";
            }
            else if (saldo <= 0.009m)
            {
                estatus = "liquidado";
            }
            else if (anticipoRestante > 0.009m)
            {
                estatus = "pendiente_anticipo";
                vencido = planAnticipo != null && planAnticipo.FechaVencimiento.Date < hoy;
            }
            else
            {
                estatus = "anticipo_pagado";
                vencido = planLiquidacion != null && planLiquidacion.FechaVencimiento.Date < hoy;
            }

            var pagos = c.Pagos
                .OrderByDescending(p => p.FechaPago)
                .ThenByDescending(p => p.Id)
                .Select(p => new PagoItemDto(
                    p.Id,
                    p.ContratoId,
                    c.Folio,
                    nombreEvento,
                    p.PlanPagoId,
                    p.PlanPago?.NumeroPago ?? 1,
                    p.Monto,
                    p.MetodoPago,
                    p.TipoTransaccion,
                    p.Referencia,
                    p.FechaPago.ToString("yyyy-MM-dd"),
                    p.Estatus,
                    p.Notas,
                    p.Ticket?.Folio,
                    clienteNombre
                ))
                .ToList();

            return new EstadoCuentaEventoDto(
                c.Id,
                c.Folio,
                nombreEvento,
                ev?.CatalogoEvento?.Categoria,
                ev?.CatalogoEvento?.ImagenUrl,
                (ev?.FechaEvento ?? c.FechaInicio).ToString("yyyy-MM-dd"),
                c.ClienteId,
                clienteNombre,
                total,
                pagado,
                saldo,
                anticipoMinimo,
                anticipoRestante,
                porcentaje,
                estatus,
                vencido,
                planAnticipo?.FechaVencimiento.ToString("yyyy-MM-dd"),
                planLiquidacion?.FechaVencimiento.ToString("yyyy-MM-dd"),
                pagos
            );
        }).ToList();
    }

    /// <summary>
    /// Anticipo mínimo requerido (50%): monto de la primera cuota del plan, o la mitad del contrato si no existe plan.
    /// </summary>
    private static decimal ObtenerAnticipoMinimo(Contrato contrato)
    {
        var primera = contrato.PlanPagos.OrderBy(pp => pp.NumeroPago).FirstOrDefault();
        return primera?.Monto ?? Math.Round(contrato.TotalContrato / 2m, 2);
    }

    public async Task<(RegistrarPagoResponse? Result, string? Error)> RegistrarPagoAsync(RegistrarPagoRequest request, CancellationToken cancellationToken = default)
    {
        const decimal tolerancia = 0.01m;

        if (request.Monto <= 0)
        {
            return (null, "El monto del pago debe ser mayor a cero.");
        }

        var contrato = await _db.Contratos
            .Include(c => c.PlanPagos)
            .FirstOrDefaultAsync(c => c.Id == request.ContratoId, cancellationToken);

        if (contrato == null)
        {
            return (null, "El contrato especificado no existe.");
        }

        if (contrato.Estatus == "cancelado")
        {
            return (null, "No es posible registrar pagos en un contrato cancelado.");
        }

        if (contrato.PlanPagos.Count == 0)
        {
            await GenerarPlanPagosDefaultAsync(contrato.Id, cancellationToken);
            // reload plan pagos
            await _db.Entry(contrato).Collection(c => c.PlanPagos).LoadAsync(cancellationToken);
        }

        decimal pagadoPrevio = await _db.Pagos
            .Where(p => p.ContratoId == contrato.Id && p.Estatus == "procesado")
            .SumAsync(p => p.Monto, cancellationToken);

        decimal saldoPendiente = Math.Max(0m, contrato.TotalContrato - pagadoPrevio);
        if (saldoPendiente <= tolerancia)
        {
            return (null, "Este evento ya se encuentra liquidado al 100%. No admite pagos adicionales.");
        }

        if (request.Monto > saldoPendiente + tolerancia)
        {
            return (null, $"No es posible realizar un pago mayor al 100% del valor del evento. El saldo pendiente máximo a liquidar es de ${saldoPendiente:N2} MXN.");
        }

        decimal anticipoMinimo = ObtenerAnticipoMinimo(contrato);
        decimal anticipoRestante = Math.Max(0m, anticipoMinimo - pagadoPrevio);
        if (anticipoRestante > tolerancia && request.Monto + tolerancia < anticipoRestante)
        {
            return (null, $"El primer pago debe cubrir al menos el 50% de anticipo (${anticipoRestante:N2} MXN).");
        }

        // El abono se asocia a la primera cuota que aún no está cubierta
        PlanPago? planPago = contrato.PlanPagos
            .Where(p => p.Estatus != "pagado" && p.Estatus != "cancelado")
            .OrderBy(p => p.NumeroPago)
            .FirstOrDefault();

        DateTime fecha = DateTime.UtcNow.Date;
        if (!string.IsNullOrWhiteSpace(request.FechaPago) && DateTime.TryParse(request.FechaPago, out var parsed))
        {
            fecha = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            string metodo = string.IsNullOrWhiteSpace(request.MetodoPago) ? "efectivo" : request.MetodoPago.ToLower().Trim();
            // El tipo de transacción se determina en servidor según lo que cubre el abono
            string tipo = request.Monto + tolerancia >= saldoPendiente
                ? "liquidacion"
                : anticipoRestante > tolerancia ? "anticipo" : "parcial";

            // Generación 100% automática de la referencia / folio de pago desde el backend
            string prefijo = metodo switch
            {
                "transferencia" => "SPEI",
                "tarjeta" or "tarjeta_credito" or "tarjeta_debito" => "TAR",
                _ => "EFE"
            };
            string fechaStr = DateTime.UtcNow.ToString("yyyyMMdd");
            string aleatorio = Random.Shared.Next(1000, 9999).ToString();
            string referenciaFinal = !string.IsNullOrWhiteSpace(request.Referencia)
                ? request.Referencia.Trim()
                : $"{prefijo}-{fechaStr}-{aleatorio}";

            var pago = new Pago
            {
                ContratoId = contrato.Id,
                PlanPagoId = planPago?.Id,
                Monto = request.Monto,
                MetodoPago = metodo,
                TipoTransaccion = tipo,
                Referencia = referenciaFinal,
                FechaPago = fecha,
                Estatus = "procesado",
                Notas = request.Notas,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Pagos.Add(pago);
            await _db.SaveChangesAsync(cancellationToken);

            int anio = DateTime.UtcNow.Year;
            int countTickets = await _db.Tickets.CountAsync(t => t.Folio.StartsWith($"TCK-{anio}-"), cancellationToken);
            string ticketFolio = $"TCK-{anio}-{(countTickets + 1):D4}";

            var ticket = new Ticket
            {
                PagoId = pago.Id,
                Folio = ticketFolio,
                EmitidoAt = DateTime.UtcNow,
                Impreso = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync(cancellationToken);

            // Distribuye el total abonado sobre las cuotas en orden (anticipo -> liquidación).
            // Así, un pago del 100% marca ambas cuotas como pagadas.
            decimal totalPagadoContrato = pagadoPrevio + pago.Monto;
            decimal acumulado = totalPagadoContrato;
            foreach (var cuota in contrato.PlanPagos.Where(p => p.Estatus != "cancelado").OrderBy(p => p.NumeroPago))
            {
                if (acumulado + tolerancia >= cuota.Monto)
                {
                    if (cuota.Estatus != "pagado")
                    {
                        cuota.Estatus = "pagado";
                        cuota.UpdatedAt = DateTime.UtcNow;
                    }
                    acumulado -= cuota.Monto;
                }
                else
                {
                    break;
                }
            }
            await _db.SaveChangesAsync(cancellationToken);

            string estatusCuotaFinal = planPago?.Estatus ?? "pendiente";

            if (totalPagadoContrato + tolerancia >= contrato.TotalContrato)
            {
                contrato.Estatus = "completado";
                contrato.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (contrato.Estatus == "activo" && totalPagadoContrato > 0)
            {
                contrato.Estatus = "en_proceso";
                contrato.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);

            return (new RegistrarPagoResponse(
                pago.Id,
                ticket.Id,
                ticket.Folio,
                contrato.Folio,
                pago.Monto,
                estatusCuotaFinal,
                "Pago registrado y procesado exitosamente."
            ), null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (null, $"Error al registrar el pago: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> GenerarPlanPagosDefaultAsync(int contratoId, CancellationToken cancellationToken = default)
    {
        var contrato = await _db.Contratos
            .Include(c => c.PlanPagos)
            .FirstOrDefaultAsync(c => c.Id == contratoId, cancellationToken);

        if (contrato == null)
        {
            return (false, "El contrato especificado no existe.");
        }

        if (contrato.PlanPagos.Count > 0)
        {
            return (true, null);
        }

        decimal total = contrato.TotalContrato;
        decimal mitad = Math.Round(total / 2m, 2);
        decimal resto = total - mitad;

        DateTime fechaAnticipo = contrato.FechaFirma.HasValue
            ? contrato.FechaFirma.Value.AddDays(5).Date
            : DateTime.UtcNow.AddDays(5).Date;

        DateTime fechaLiquidacion = contrato.FechaInicio.AddDays(-7).Date;
        if (fechaLiquidacion <= fechaAnticipo)
        {
            fechaLiquidacion = contrato.FechaInicio.Date;
        }

        var cuota1 = new PlanPago
        {
            ContratoId = contrato.Id,
            NumeroPago = 1,
            Monto = mitad,
            FechaVencimiento = DateTime.SpecifyKind(fechaAnticipo, DateTimeKind.Utc),
            Estatus = "pendiente",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var cuota2 = new PlanPago
        {
            ContratoId = contrato.Id,
            NumeroPago = 2,
            Monto = resto,
            FechaVencimiento = DateTime.SpecifyKind(fechaLiquidacion, DateTimeKind.Utc),
            Estatus = "pendiente",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.PlanPagos.Add(cuota1);
        _db.PlanPagos.Add(cuota2);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}
