using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Core.DTOs;
using Server.Core.Entities;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Infrastructure.Services;

public class CatalogoService : ICatalogoService
{
    private readonly AppDbContext _db;

    public CatalogoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CatalogoEvento>> GetCatalogoEventosAsync(bool incluirInactivos = false, CancellationToken cancellationToken = default)
    {
        var query = _db.CatalogoEventos.AsQueryable();
        if (!incluirInactivos)
        {
            query = query.Where(e => e.Activo);
        }

        return await query
            .OrderBy(e => e.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogoEvento?> GetCatalogoEventoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.CatalogoEventos
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(CatalogoEvento? Result, string? Error)> CrearCatalogoEventoAsync(CrearCatalogoEventoRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return (null, "El nombre del evento es obligatorio.");
        }

        if (request.PrecioBase < 0)
        {
            return (null, "El precio base no puede ser negativo.");
        }

        if (request.DuracionHoras <= 0)
        {
            return (null, "La duración en horas debe ser mayor a 0.");
        }

        var utcNow = DateTime.UtcNow;
        var nuevoEvento = new CatalogoEvento
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            PrecioBase = request.PrecioBase,
            DuracionHoras = request.DuracionHoras,
            Activo = request.Activo ?? true,
            ImagenUrl = request.ImagenUrl?.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _db.CatalogoEventos.Add(nuevoEvento);
        await _db.SaveChangesAsync(cancellationToken);

        return (nuevoEvento, null);
    }

    public async Task<(CatalogoEvento? Result, string? Error)> ActualizarCatalogoEventoAsync(int id, ActualizarCatalogoEventoRequest request, CancellationToken cancellationToken = default)
    {
        var evento = await _db.CatalogoEventos.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (evento is null)
        {
            return (null, "Evento de catálogo no encontrado.");
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return (null, "El nombre del evento es obligatorio.");
        }

        if (request.PrecioBase < 0)
        {
            return (null, "El precio base no puede ser negativo.");
        }

        if (request.DuracionHoras <= 0)
        {
            return (null, "La duración en horas debe ser mayor a 0.");
        }

        evento.Nombre = request.Nombre.Trim();
        evento.Descripcion = request.Descripcion?.Trim();
        evento.PrecioBase = request.PrecioBase;
        evento.DuracionHoras = request.DuracionHoras;
        if (!string.IsNullOrWhiteSpace(request.ImagenUrl) && request.ImagenUrl.Trim() != evento.ImagenUrl)
        {
            if (!string.IsNullOrWhiteSpace(evento.ImagenUrl))
            {
                EliminarArchivoImagen(evento.ImagenUrl);
            }
            evento.ImagenUrl = request.ImagenUrl.Trim();
        }
        if (request.Activo.HasValue)
        {
            evento.Activo = request.Activo.Value;
        }
        evento.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (evento, null);
    }

    public async Task<bool> DeleteCatalogoEventoAsync(int id, CancellationToken cancellationToken = default)
    {
        var evento = await _db.CatalogoEventos.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (evento is null)
        {
            return false;
        }

        // Desvincular de cotizacion_items (set CatalogoEventoId = null) para evitar violaciones de clave foránea
        var itemsRelacionados = await _db.CotizacionItems
            .Where(ci => ci.CatalogoEventoId == id)
            .ToListAsync(cancellationToken);

        foreach (var item in itemsRelacionados)
        {
            item.CatalogoEventoId = null;
        }

        if (!string.IsNullOrWhiteSpace(evento.ImagenUrl))
        {
            EliminarArchivoImagen(evento.ImagenUrl);
        }

        _db.CatalogoEventos.Remove(evento);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"[CatalogoService] No se pudo eliminar el evento {id} de Supabase por restricciones de integridad: {ex.Message}");
            return false;
        }
    }

    private void EliminarArchivoImagen(string? imagenUrl)
    {
        if (string.IsNullOrWhiteSpace(imagenUrl))
            return;

        try
        {
            string relativePath = imagenUrl.Trim();
            if (Uri.TryCreate(relativePath, UriKind.Absolute, out var uri))
            {
                relativePath = uri.AbsolutePath;
            }

            relativePath = relativePath.TrimStart('/', '\\');
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CatalogoService] No se pudo eliminar el archivo de imagen '{imagenUrl}': {ex.Message}");
        }
    }

    // ==========================================
    // SERVICIOS DE CATÁLOGO
    // ==========================================

    public async Task<List<CatalogoServicio>> GetCatalogoServiciosAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CatalogoServicios
            .Where(s => s.Activo)
            .OrderBy(s => s.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogoServicio?> GetCatalogoServicioByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.CatalogoServicios
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<(CatalogoServicio? Result, string? Error)> CrearCatalogoServicioAsync(CrearCatalogoServicioRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return (null, "El nombre del servicio es obligatorio.");
        }

        if (request.PrecioBase < 0)
        {
            return (null, "El precio base no puede ser negativo.");
        }

        var utcNow = DateTime.UtcNow;
        var nuevoServicio = new CatalogoServicio
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            PrecioBase = request.PrecioBase,
            Unidad = !string.IsNullOrWhiteSpace(request.Unidad) ? request.Unidad.Trim() : "unidad",
            Activo = request.Activo ?? true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _db.CatalogoServicios.Add(nuevoServicio);
        await _db.SaveChangesAsync(cancellationToken);

        return (nuevoServicio, null);
    }

    public async Task<(CatalogoServicio? Result, string? Error)> ActualizarCatalogoServicioAsync(int id, ActualizarCatalogoServicioRequest request, CancellationToken cancellationToken = default)
    {
        var servicio = await _db.CatalogoServicios.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (servicio is null)
        {
            return (null, "Servicio de catálogo no encontrado.");
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return (null, "El nombre del servicio es obligatorio.");
        }

        if (request.PrecioBase < 0)
        {
            return (null, "El precio base no puede ser negativo.");
        }

        servicio.Nombre = request.Nombre.Trim();
        servicio.Descripcion = request.Descripcion?.Trim();
        servicio.PrecioBase = request.PrecioBase;
        if (!string.IsNullOrWhiteSpace(request.Unidad))
        {
            servicio.Unidad = request.Unidad.Trim();
        }
        if (request.Activo.HasValue)
        {
            servicio.Activo = request.Activo.Value;
        }
        servicio.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (servicio, null);
    }

    public async Task<bool> DeleteCatalogoServicioAsync(int id, CancellationToken cancellationToken = default)
    {
        var servicio = await _db.CatalogoServicios.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (servicio is null)
        {
            return false;
        }

        servicio.Activo = false;
        servicio.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}


