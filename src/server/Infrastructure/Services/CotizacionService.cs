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
        decimal extraHorasMonto = request.HorasAdicionales > 0 ? request.HorasAdicionales * 1000m : 0m;
        decimal totalCotizacion = totalBase + extraHorasMonto;

        // Generar un folio único: COT-YYYY-XXXX
        int randomNum = Random.Shared.Next(1000, 9999);
        string folio = $"COT-{DateTime.UtcNow.Year}-{randomNum}";

        DateTime? fechaEventoParsed = null;
        if (DateTime.TryParse(request.FechaEvento, out var parsedDate))
        {
            fechaEventoParsed = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }

        var nuevaCotizacion = new Cotizacion
        {
            ClienteId = targetClienteId,
            Folio = folio,
            Total = totalCotizacion,
            Descuento = 0m,
            Estatus = "enviada",
            FechaVigencia = DateTime.UtcNow.AddDays(30),
            FechaEvento = fechaEventoParsed,
            Invitados = request.Invitados,
            Notas = request.Notas,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Cotizaciones.Add(nuevaCotizacion);
        await _db.SaveChangesAsync(cancellationToken);

        var itemEvento = new CotizacionItem
        {
            CotizacionId = nuevaCotizacion.Id,
            Tipo = "evento",
            Cantidad = 1,
            PrecioUnitario = evento.PrecioBase,
            DescuentoItem = 0m,
            Notas = $"Paquete {evento.Nombre} - Invitados: {request.Invitados}" + (request.HorasAdicionales > 0 ? $", Horas extra: {request.HorasAdicionales}" : ""),
            CatalogoEventoId = evento.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CotizacionItems.Add(itemEvento);

        if (request.HorasAdicionales > 0)
        {
            var itemHoras = new CotizacionItem
            {
                CotizacionId = nuevaCotizacion.Id,
                Tipo = "evento",
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
            new CotizacionItemDto(itemEvento.Id, $"Paquete {evento.Nombre}", evento.PrecioBase)
        };
        if (request.HorasAdicionales > 0)
        {
            itemsDto.Add(new CotizacionItemDto(0, $"Horas adicionales ({request.HorasAdicionales} hrs)", extraHorasMonto));
        }

        var responseDto = new CotizacionResponse(
            nuevaCotizacion.Id,
            nuevaCotizacion.Folio,
            evento.Nombre,
            evento.Id,
            nuevaCotizacion.FechaEvento?.ToString("yyyy-MM-dd"),
            nuevaCotizacion.Invitados,
            nuevaCotizacion.Total,
            nuevaCotizacion.Descuento,
            nuevaCotizacion.Total,
            nuevaCotizacion.Estatus,
            nuevaCotizacion.FechaVigencia.ToString("yyyy-MM-dd"),
            nuevaCotizacion.CreatedAt.ToString("yyyy-MM-dd"),
            nuevaCotizacion.Notas ?? "",
            itemsDto
        );

        return (responseDto, null);
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

        var itemsDto = c.Items.Select(i => new CotizacionItemDto(
            i.Id,
            !string.IsNullOrWhiteSpace(i.Notas) ? i.Notas : (i.CatalogoEvento != null ? $"Paquete {i.CatalogoEvento.Nombre}" : "Servicio"),
            i.Subtotal ?? (i.Cantidad * i.PrecioUnitario - i.DescuentoItem)
        )).ToList();

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
