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

public class CotizacionService : ICotizacionService
{
    private readonly AppDbContext _db;

    public CotizacionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CotizacionResponse>> GetCotizacionesAsync(int? clienteId, CancellationToken cancellationToken = default)
    {
        await ExpirarBorradoresAntiguosAsync(cancellationToken);

        var query = _db.Cotizaciones
            .Include(c => c.Cliente)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoEvento)
            .AsQueryable();

        if (clienteId.HasValue && clienteId.Value > 0)
        {
            query = query.Where(c => c.ClienteId == clienteId.Value);
        }

        var cotizaciones = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return cotizaciones.Select(MapToResponse).ToList();
    }

    public async Task<CotizacionResponse?> GetCotizacionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await ExpirarBorradoresAntiguosAsync(cancellationToken);

        var c = await _db.Cotizaciones
            .Include(c => c.Cliente)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoEvento)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (c is null) return null;

        return MapToResponse(c);
    }

    public async Task<(CotizacionResponse? Result, string? Error)> CrearCotizacionAsync(CrearCotizacionRequest request, CancellationToken cancellationToken = default)
    {
        await ExpirarBorradoresAntiguosAsync(cancellationToken);

        var evento = await _db.CatalogoEventos.FindAsync(new object[] { request.CatalogoEventoId }, cancellationToken);
        if (evento is null)
        {
            return (null, "El evento especificado no existe en el catálogo.");
        }

        int targetClienteId = request.ClienteId ?? 0;
        if (targetClienteId <= 0)
        {
            var primerCliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Activo, cancellationToken);
            if (primerCliente != null)
            {
                targetClienteId = primerCliente.Id;
            }
            else
            {
                return (null, "No hay clientes activos registrados.");
            }
        }

        decimal totalBase = evento.PrecioBase;
        int capacidadBase = evento.CapacidadPersonas > 0 ? evento.CapacidadPersonas : 50;
        int extraInvitados = Math.Max(0, request.Invitados - capacidadBase);
        decimal extraInvitadosMonto = extraInvitados * 150m;
        decimal extraHorasMonto = request.HorasAdicionales > 0 ? request.HorasAdicionales * 1000m : 0m;
        decimal totalCotizacion = totalBase + extraHorasMonto + extraInvitadosMonto;

        string estatusFinal = !string.IsNullOrWhiteSpace(request.Estatus) ? request.Estatus.ToLower() : "enviada";

        DateTime? fechaEventoParsed = null;
        if (DateTime.TryParse(request.FechaEvento, out var parsedDate))
        {
            fechaEventoParsed = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }

        Cotizacion cotizacionDestino;

        if (estatusFinal == "borrador")
        {
            var borradorExistente = await _db.Cotizaciones
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.ClienteId == targetClienteId && c.Estatus == "borrador" && c.Items.Any(i => i.CatalogoEventoId == request.CatalogoEventoId), cancellationToken);

            if (borradorExistente != null)
            {
                cotizacionDestino = borradorExistente;
                cotizacionDestino.Total = totalCotizacion;
                cotizacionDestino.FechaEvento = fechaEventoParsed;
                cotizacionDestino.Invitados = request.Invitados;
                cotizacionDestino.Notas = request.Notas;
                cotizacionDestino.UpdatedAt = DateTime.UtcNow;

                _db.CotizacionItems.RemoveRange(cotizacionDestino.Items);
                cotizacionDestino.Items.Clear();
            }
            else
            {
                int randomNum = Random.Shared.Next(1000, 9999);
                string folio = $"COT-{DateTime.UtcNow.Year}-{randomNum}";
                cotizacionDestino = new Cotizacion
                {
                    ClienteId = targetClienteId,
                    Folio = folio,
                    Total = totalCotizacion,
                    Descuento = 0m,
                    Estatus = estatusFinal,
                    FechaVigencia = DateTime.UtcNow.AddDays(30),
                    FechaEvento = fechaEventoParsed,
                    Invitados = request.Invitados,
                    Notas = request.Notas,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Cotizaciones.Add(cotizacionDestino);
            }
        }
        else
        {
            int randomNum = Random.Shared.Next(1000, 9999);
            string folio = $"COT-{DateTime.UtcNow.Year}-{randomNum}";
            cotizacionDestino = new Cotizacion
            {
                ClienteId = targetClienteId,
                Folio = folio,
                Total = totalCotizacion,
                Descuento = 0m,
                Estatus = estatusFinal,
                FechaVigencia = DateTime.UtcNow.AddDays(30),
                FechaEvento = fechaEventoParsed,
                Invitados = request.Invitados,
                Notas = request.Notas,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Cotizaciones.Add(cotizacionDestino);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var itemEvento = new CotizacionItem
        {
            CotizacionId = cotizacionDestino.Id,
            Tipo = "evento",
            Cantidad = 1,
            PrecioUnitario = evento.PrecioBase,
            DescuentoItem = 0m,
            Notas = $"Paquete {evento.Nombre} - Base: {capacidadBase} personas",
            CatalogoEventoId = evento.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CotizacionItems.Add(itemEvento);

        if (extraInvitados > 0)
        {
            var itemExtraInvitados = new CotizacionItem
            {
                CotizacionId = cotizacionDestino.Id,
                Tipo = "servicio",
                Cantidad = (short)extraInvitados,
                PrecioUnitario = 150m,
                DescuentoItem = 0m,
                Notas = $"Invitados adicionales ({extraInvitados} personas)",
                CatalogoEventoId = evento.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.CotizacionItems.Add(itemExtraInvitados);
        }

        if (request.HorasAdicionales > 0)
        {
            var itemHoras = new CotizacionItem
            {
                CotizacionId = cotizacionDestino.Id,
                Tipo = "servicio",
                Cantidad = (short)request.HorasAdicionales,
                PrecioUnitario = 1000m,
                DescuentoItem = 0m,
                Notas = $"Horas adicionales ({request.HorasAdicionales} hrs)",
                CatalogoEventoId = evento.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.CotizacionItems.Add(itemHoras);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var itemsDto = new List<CotizacionItemDto>
        {
            new CotizacionItemDto(itemEvento.Id, "evento", $"Paquete {evento.Nombre} (Base {capacidadBase} pers.)", evento.PrecioBase, 1, 0m, evento.PrecioBase, null)
        };
        if (extraInvitados > 0)
        {
            itemsDto.Add(new CotizacionItemDto(0, "servicio", $"Invitados adicionales ({extraInvitados} pers.)", 150m, extraInvitados, 0m, extraInvitadosMonto, null));
        }
        if (request.HorasAdicionales > 0)
        {
            itemsDto.Add(new CotizacionItemDto(0, "servicio", $"Horas adicionales ({request.HorasAdicionales} hrs)", 1000m, request.HorasAdicionales, 0m, extraHorasMonto, null));
        }

        var responseDto = new CotizacionResponse(
            cotizacionDestino.Id,
            cotizacionDestino.Folio,
            evento.Nombre,
            evento.Id,
            cotizacionDestino.FechaEvento?.ToString("yyyy-MM-dd"),
            cotizacionDestino.Invitados,
            cotizacionDestino.Total,
            cotizacionDestino.Descuento,
            cotizacionDestino.Total,
            cotizacionDestino.Estatus,
            cotizacionDestino.FechaVigencia.ToString("yyyy-MM-dd"),
            cotizacionDestino.CreatedAt.ToString("yyyy-MM-dd"),
            cotizacionDestino.Notas ?? "",
            itemsDto
        );

        return (responseDto, null);
    }

    private async Task ExpirarBorradoresAntiguosAsync(CancellationToken cancellationToken)
    {
        try
        {
            var fechaLimite = DateTime.UtcNow.AddDays(-30);
            var borradoresVencidos = await _db.Cotizaciones
                .Where(c => c.Estatus == "borrador" && c.UpdatedAt < fechaLimite)
                .ToListAsync(cancellationToken);

            if (borradoresVencidos.Count > 0)
            {
                foreach (var cot in borradoresVencidos)
                {
                    cot.Estatus = "vencida";
                    cot.UpdatedAt = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log expiration error silently without failing the main request
            System.Diagnostics.Debug.WriteLine($"Error en ExpirarBorradoresAntiguos: {ex.Message}");
        }
    }

    public async Task<(CotizacionResponse? Result, string? Error)> ActualizarCotizacionAsync(int id, CrearCotizacionRequest request, CancellationToken cancellationToken = default)
    {
        await ExpirarBorradoresAntiguosAsync(cancellationToken);

        var cotizacion = await _db.Cotizaciones
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cotizacion is null)
        {
            return (null, "La cotización especificada no existe.");
        }

        if (cotizacion.Estatus != "borrador")
        {
            return (null, "Solo se pueden editar cotizaciones que estén en estatus borrador.");
        }

        var evento = await _db.CatalogoEventos.FindAsync(new object[] { request.CatalogoEventoId }, cancellationToken);
        if (evento is null)
        {
            return (null, "El evento especificado no existe en el catálogo.");
        }

        decimal totalBase = evento.PrecioBase;
        int capacidadBase = evento.CapacidadPersonas > 0 ? evento.CapacidadPersonas : 50;
        int extraInvitados = Math.Max(0, request.Invitados - capacidadBase);
        decimal extraInvitadosMonto = extraInvitados * 150m;
        decimal extraHorasMonto = request.HorasAdicionales > 0 ? request.HorasAdicionales * 1000m : 0m;
        decimal totalCotizacion = totalBase + extraHorasMonto + extraInvitadosMonto;

        string estatusFinal = !string.IsNullOrWhiteSpace(request.Estatus) ? request.Estatus.ToLower() : cotizacion.Estatus;

        DateTime? fechaEventoParsed = null;
        if (DateTime.TryParse(request.FechaEvento, out var parsedDate))
        {
            fechaEventoParsed = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }

        cotizacion.Total = totalCotizacion;
        cotizacion.Estatus = estatusFinal;
        cotizacion.FechaEvento = fechaEventoParsed;
        cotizacion.Invitados = request.Invitados;
        cotizacion.Notas = request.Notas;
        cotizacion.UpdatedAt = DateTime.UtcNow;

        _db.CotizacionItems.RemoveRange(cotizacion.Items);
        cotizacion.Items.Clear();

        var itemEvento = new CotizacionItem
        {
            CotizacionId = cotizacion.Id,
            Tipo = "evento",
            Cantidad = 1,
            PrecioUnitario = evento.PrecioBase,
            DescuentoItem = 0m,
            Notas = $"Paquete {evento.Nombre} - Base: {capacidadBase} personas",
            CatalogoEventoId = evento.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.CotizacionItems.Add(itemEvento);

        if (extraInvitados > 0)
        {
            var itemExtraInvitados = new CotizacionItem
            {
                CotizacionId = cotizacion.Id,
                Tipo = "servicio",
                Cantidad = (short)extraInvitados,
                PrecioUnitario = 150m,
                DescuentoItem = 0m,
                Notas = $"Invitados adicionales ({extraInvitados} personas)",
                CatalogoEventoId = evento.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.CotizacionItems.Add(itemExtraInvitados);
        }

        if (request.HorasAdicionales > 0)
        {
            var itemHoras = new CotizacionItem
            {
                CotizacionId = cotizacion.Id,
                Tipo = "servicio",
                Cantidad = (short)request.HorasAdicionales,
                PrecioUnitario = 1000m,
                DescuentoItem = 0m,
                Notas = $"Horas adicionales ({request.HorasAdicionales} hrs)",
                CatalogoEventoId = evento.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.CotizacionItems.Add(itemHoras);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var cActualizada = await _db.Cotizaciones
            .Include(c => c.Cliente)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoEvento)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return (MapToResponse(cActualizada!), null);
    }

    public async Task<bool> DeleteCotizacionAsync(int id, CancellationToken cancellationToken = default)
    {
        var cotizacion = await _db.Cotizaciones.FindAsync(new object[] { id }, cancellationToken);
        if (cotizacion is null) return false;

        _db.Cotizaciones.Remove(cotizacion);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CotizacionResponse MapToResponse(Cotizacion c)
    {
        var primerItemEvento = c.Items.FirstOrDefault(i => i.Tipo == "evento");
        var nombreEvento = primerItemEvento?.CatalogoEvento?.Nombre ?? "Evento personalizado";

        var itemsDto = c.Items.Select(i =>
        {
            string desc = !string.IsNullOrWhiteSpace(i.Notas)
                ? i.Notas
                : (i.CatalogoEvento != null ? $"Paquete {i.CatalogoEvento.Nombre}" : (i.CatalogoServicio != null ? i.CatalogoServicio.Nombre : "Servicio contratado"));

            decimal subtotal = i.Subtotal ?? (i.Cantidad * i.PrecioUnitario - i.DescuentoItem);

            return new CotizacionItemDto(
                i.Id,
                i.Tipo ?? "evento",
                desc,
                i.PrecioUnitario,
                i.Cantidad,
                i.DescuentoItem,
                subtotal,
                i.Notas
            );
        }).ToList();

        return new CotizacionResponse(
            c.Id,
            c.Folio,
            nombreEvento,
            primerItemEvento?.CatalogoEventoId,
            c.FechaEvento?.ToString("yyyy-MM-dd"),
            c.Invitados,
            c.Total,
            c.Descuento,
            c.TotalFinal ?? (c.Total - c.Descuento),
            c.Estatus,
            c.FechaVigencia.ToString("yyyy-MM-dd"),
            c.CreatedAt.ToString("yyyy-MM-dd"),
            c.Notas ?? "",
            itemsDto
        );
    }
}
