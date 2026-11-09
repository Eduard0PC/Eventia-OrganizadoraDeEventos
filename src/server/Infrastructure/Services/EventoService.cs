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

public class EventoService : IEventoService
{
    private readonly AppDbContext _db;

    public EventoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EventoContratadoResponse>> GetEventosContratadosAsync(int? clienteId, CancellationToken cancellationToken = default)
    {
        var query = _db.EventosContratados
            .Include(ev => ev.Contrato)
                .ThenInclude(c => c.Cliente)
            .Include(ev => ev.CatalogoEvento)
            .AsQueryable();

        if (clienteId.HasValue && clienteId.Value > 0)
        {
            query = query.Where(ev => ev.Contrato.ClienteId == clienteId.Value);
        }

        var eventos = await query
            .OrderByDescending(ev => ev.FechaEvento)
            .ThenByDescending(ev => ev.CreatedAt)
            .ToListAsync(cancellationToken);

        return eventos.Select(MapToResponse).ToList();
    }

    public async Task<EventoContratadoResponse?> GetEventoContratadoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var ev = await _db.EventosContratados
            .Include(e => e.Contrato)
                .ThenInclude(c => c.Cliente)
            .Include(e => e.CatalogoEvento)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (ev is null) return null;
        return MapToResponse(ev);
    }

    public async Task<(EventoContratadoResponse? Result, string? Error)> ActualizarEstatusEventoAsync(int id, string estatus, string? notas, CancellationToken cancellationToken = default)
    {
        var ev = await _db.EventosContratados
            .Include(e => e.Contrato)
                .ThenInclude(c => c.Cliente)
            .Include(e => e.CatalogoEvento)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (ev is null)
        {
            return (null, "El evento contratado especificado no existe.");
        }

        ev.Estatus = estatus.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(notas))
        {
            ev.Notas = string.IsNullOrWhiteSpace(ev.Notas)
                ? notas.Trim()
                : $"{ev.Notas}\n{notas.Trim()}";
        }
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (MapToResponse(ev), null);
    }

    public async Task<ContratoDocumentoDto?> GetContratoDocumentoByEventoIdAsync(int eventoContratadoId, CancellationToken cancellationToken = default)
    {
        var ev = await _db.EventosContratados
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventoContratadoId, cancellationToken);

        if (ev is null) return null;

        return await BuildContratoDocumentoAsync(ev.ContratoId, ev.Id, cancellationToken);
    }

    public async Task<ContratoDocumentoDto?> GetContratoDocumentoByContratoIdAsync(int contratoId, CancellationToken cancellationToken = default)
    {
        return await BuildContratoDocumentoAsync(contratoId, null, cancellationToken);
    }

    private async Task<ContratoDocumentoDto?> BuildContratoDocumentoAsync(int contratoId, int? preferredEventoId, CancellationToken cancellationToken)
    {
        var contrato = await _db.Contratos
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.EventosContratados)
                .ThenInclude(ev => ev.CatalogoEvento)
            .Include(c => c.Cotizacion)
                .ThenInclude(cot => cot!.Items)
                    .ThenInclude(i => i.CatalogoEvento)
            .Include(c => c.Cotizacion)
                .ThenInclude(cot => cot!.Items)
                    .ThenInclude(i => i.CatalogoServicio!)
                        .ThenInclude(cs => cs.CategoriaProveedor)
            .Include(c => c.PlanPagos)
            .Include(c => c.Pagos)
            .FirstOrDefaultAsync(c => c.Id == contratoId, cancellationToken);

        if (contrato is null) return null;

        var ev = (preferredEventoId.HasValue
            ? contrato.EventosContratados.FirstOrDefault(e => e.Id == preferredEventoId.Value)
            : null) ?? contrato.EventosContratados.FirstOrDefault();

        var catalogoEv = ev?.CatalogoEvento
            ?? contrato.Cotizacion?.Items.FirstOrDefault(i => i.CatalogoEvento != null)?.CatalogoEvento;

        string clienteNombreCompleto = contrato.Cliente != null
            ? $"{contrato.Cliente.Nombre} {contrato.Cliente.Apellido}".Trim()
            : "Cliente Contratante";

        string nombreEvento = catalogoEv?.Nombre ?? "Evento Social / Corporativo";
        string? categoriaEvento = catalogoEv?.Categoria;
        string? descripcionEvento = catalogoEv?.Descripcion;
        short duracionHoras = catalogoEv?.DuracionHoras ?? 5;
        int capacidadBase = catalogoEv?.CapacidadPersonas ?? 50;
        decimal precioBaseEvento = catalogoEv?.PrecioBase ?? contrato.TotalContrato;

        string fechaEvento = ev?.FechaEvento.ToString("yyyy-MM-dd")
            ?? contrato.FechaInicio.ToString("yyyy-MM-dd");

        string horaInicio = ev?.HoraInicio?.ToString(@"hh\:mm") ?? "18:00";
        string horaFin = ev?.HoraFin?.ToString(@"hh\:mm") ?? "23:00";
        string lugar = !string.IsNullOrWhiteSpace(ev?.Lugar) ? ev.Lugar : "Salón Principal / Por confirmar con cliente";
        int aforo = ev?.Aforo ?? contrato.Cotizacion?.Invitados ?? capacidadBase;
        string estatusEvento = ev?.Estatus ?? "programado";
        string? notasEvento = ev?.Notas ?? contrato.Condiciones ?? contrato.Cotizacion?.Notas;

        var itemsDto = new List<CotizacionItemDto>();
        if (contrato.Cotizacion?.Items != null && contrato.Cotizacion.Items.Count > 0)
        {
            itemsDto = contrato.Cotizacion.Items
                .OrderBy(i => i.Tipo == "evento" ? 0 : 1)
                .ThenBy(i => i.Id)
                .Select(i =>
                {
                    string desc = !string.IsNullOrWhiteSpace(i.Notas)
                        ? i.Notas
                        : (i.CatalogoEvento != null
                            ? $"Paquete Base: {i.CatalogoEvento.Nombre}"
                            : (i.CatalogoServicio != null ? i.CatalogoServicio.Nombre : "Servicio contratado"));

                    decimal sub = i.Subtotal ?? (i.Cantidad * i.PrecioUnitario - i.DescuentoItem);
                    bool esExtra = i.Tipo == "servicio" && i.CatalogoServicio != null && i.CatalogoServicio.VisibleCliente;
                    string? catProv = i.CatalogoServicio?.CategoriaProveedor?.Nombre;

                    return new CotizacionItemDto(
                        i.Id,
                        i.Tipo ?? "evento",
                        desc,
                        i.PrecioUnitario,
                        i.Cantidad,
                        i.DescuentoItem,
                        sub,
                        i.Notas,
                        i.CatalogoServicioId,
                        esExtra,
                        catProv
                    );
                })
                .ToList();
        }
        else
        {
            itemsDto.Add(new CotizacionItemDto(
                1,
                "evento",
                $"Paquete Integral: {nombreEvento} ({aforo} invitados)",
                contrato.TotalContrato,
                1,
                0m,
                contrato.TotalContrato,
                notasEvento,
                null,
                false,
                null
            ));
        }

        var serviciosIncluidos = new List<ServicioDefectoDto>();
        if (!string.IsNullOrWhiteSpace(categoriaEvento))
        {
            var catNorm = categoriaEvento.Trim().ToLower();
            serviciosIncluidos = await _db.CatalogoEventoServicios
                .AsNoTracking()
                .Include(ces => ces.CatalogoServicio)
                    .ThenInclude(cs => cs!.CategoriaProveedor)
                .Where(ces => ces.Categoria.ToLower() == catNorm)
                .OrderBy(ces => ces.OrdenPrioridad)
                .Select(ces => new ServicioDefectoDto(
                    ces.Id,
                    ces.CatalogoServicioId,
                    ces.CatalogoServicio != null ? ces.CatalogoServicio.Nombre : "Servicio Incluido",
                    ces.CatalogoServicio != null ? ces.CatalogoServicio.Descripcion : null,
                    ces.Categoria,
                    ces.CatalogoServicio != null && ces.CatalogoServicio.CategoriaProveedor != null
                        ? ces.CatalogoServicio.CategoriaProveedor.Nombre
                        : null,
                    ces.EsObligatorio,
                    ces.IncluidoEnPrecio,
                    ces.OrdenPrioridad,
                    ces.Notas
                ))
                .ToListAsync(cancellationToken);
        }

        var planPagosDto = new List<ContratoPlanPagoDto>();
        if (contrato.PlanPagos != null && contrato.PlanPagos.Count > 0)
        {
            planPagosDto = contrato.PlanPagos
                .OrderBy(pp => pp.NumeroPago)
                .Select(pp => new ContratoPlanPagoDto(
                    pp.Id,
                    pp.NumeroPago,
                    pp.NumeroPago == 1
                        ? "Anticipo de Reservación y Garantía (50%)"
                        : $"Liquidación Total del Evento (Pago #{pp.NumeroPago})",
                    pp.Monto,
                    pp.FechaVencimiento.ToString("yyyy-MM-dd"),
                    pp.Estatus
                ))
                .ToList();
        }
        else
        {
            decimal anticipo = Math.Round(contrato.TotalContrato / 2m, 2);
            decimal liquidacion = contrato.TotalContrato - anticipo;
            DateTime fechaFirmaDt = contrato.FechaFirma ?? contrato.CreatedAt.Date;
            DateTime fechaLiquidacionDt = contrato.FechaInicio.AddDays(-7);
            if (fechaLiquidacionDt < fechaFirmaDt) fechaLiquidacionDt = contrato.FechaInicio;

            planPagosDto.Add(new ContratoPlanPagoDto(
                1,
                1,
                "Anticipo de Reservación y Garantía (50%)",
                anticipo,
                fechaFirmaDt.AddDays(5).ToString("yyyy-MM-dd"),
                "pendiente"
            ));
            planPagosDto.Add(new ContratoPlanPagoDto(
                2,
                2,
                "Liquidación Total del Evento (50%)",
                liquidacion,
                fechaLiquidacionDt.ToString("yyyy-MM-dd"),
                "pendiente"
            ));
        }

        decimal pagadoPorPagos = contrato.Pagos?
            .Where(p => p.Estatus == "procesado")
            .Sum(p => p.Monto) ?? 0m;

        decimal pagadoPorPlan = contrato.PlanPagos?
            .Where(pp => pp.Estatus == "pagado")
            .Sum(pp => pp.Monto) ?? 0m;

        decimal totalPagado = Math.Max(pagadoPorPagos, pagadoPorPlan);
        if (contrato.Estatus == "completado" && totalPagado < contrato.TotalContrato)
        {
            totalPagado = contrato.TotalContrato;
        }
        decimal saldoPendiente = Math.Max(0m, contrato.TotalContrato - totalPagado);

        decimal subtotalCotizado = contrato.Cotizacion?.Total ?? contrato.TotalContrato;
        decimal descuento = contrato.Cotizacion?.Descuento ?? 0m;

        return new ContratoDocumentoDto(
            contrato.Id,
            contrato.Folio,
            contrato.CotizacionId,
            contrato.Cotizacion?.Folio,
            (contrato.FechaFirma ?? contrato.CreatedAt.Date).ToString("yyyy-MM-dd"),
            contrato.FechaInicio.ToString("yyyy-MM-dd"),
            contrato.FechaFin.ToString("yyyy-MM-dd"),
            contrato.Estatus,
            subtotalCotizado,
            descuento,
            contrato.TotalContrato,
            totalPagado,
            saldoPendiente,
            contrato.Condiciones ?? contrato.Cotizacion?.Notas,
            contrato.ArchivoUrl,
            contrato.ClienteId,
            clienteNombreCompleto,
            contrato.Cliente?.Email ?? "contacto@cliente.com",
            contrato.Cliente?.Telefono,
            ev?.Id,
            nombreEvento,
            categoriaEvento,
            descripcionEvento,
            duracionHoras,
            capacidadBase,
            precioBaseEvento,
            fechaEvento,
            horaInicio,
            horaFin,
            lugar,
            aforo,
            estatusEvento,
            notasEvento,
            itemsDto,
            serviciosIncluidos,
            planPagosDto
        );
    }

    private static EventoContratadoResponse MapToResponse(EventoContratado ev)
    {
        string clienteNombre = ev.Contrato?.Cliente != null
            ? $"{ev.Contrato.Cliente.Nombre} {ev.Contrato.Cliente.Apellido}".Trim()
            : "Cliente";

        return new EventoContratadoResponse(
            ev.Id,
            ev.ContratoId,
            ev.Contrato?.Folio ?? $"CTR-{ev.ContratoId}",
            ev.CatalogoEventoId,
            ev.CatalogoEvento?.Nombre ?? "Evento",
            ev.CatalogoEvento?.Categoria,
            ev.Contrato?.ClienteId ?? 0,
            clienteNombre,
            ev.FechaEvento.ToString("yyyy-MM-dd"),
            ev.HoraInicio?.ToString(@"hh\:mm"),
            ev.HoraFin?.ToString(@"hh\:mm"),
            ev.Lugar ?? "Por confirmar",
            ev.Aforo,
            ev.Estatus,
            ev.Notas,
            ev.Contrato?.TotalContrato ?? 0m,
            ev.CatalogoEvento?.ImagenUrl
        );
    }
}
