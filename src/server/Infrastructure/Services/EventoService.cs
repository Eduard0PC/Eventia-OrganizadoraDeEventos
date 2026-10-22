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
