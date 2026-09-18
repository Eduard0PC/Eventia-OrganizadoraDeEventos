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

public class ProveedorService : IProveedorService
{
    private readonly AppDbContext _db;

    public ProveedorService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProveedorResponseDto>> GetProveedoresAsync(int? categoriaId = null, bool incluirInactivos = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Proveedores
            .Include(p => p.Categoria)
            .Include(p => p.Servicios)
            .AsQueryable();

        if (!incluirInactivos)
        {
            query = query.Where(p => p.Activo);
        }

        if (categoriaId.HasValue && categoriaId.Value > 0)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(MapToResponse).ToList();
    }

    public async Task<ProveedorResponseDto?> GetProveedorByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var p = await _db.Proveedores
            .Include(p => p.Categoria)
            .Include(p => p.Servicios)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return p is null ? null : MapToResponse(p);
    }

    public async Task<(ProveedorResponseDto? Result, string? Error)> CrearProveedorAsync(CrearProveedorRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return (null, "El nombre del proveedor es obligatorio.");
        }

        var categoria = await _db.CategoriasProveedores.FindAsync(new object[] { request.CategoriaId }, cancellationToken);
        if (categoria is null)
        {
            return (null, "La categoría especificada no existe.");
        }

        var proveedor = new Proveedor
        {
            CategoriaId = request.CategoriaId,
            Nombre = request.Nombre.Trim(),
            Contacto = request.Contacto?.Trim(),
            Email = request.Email?.Trim(),
            Telefono = request.Telefono?.Trim(),
            Direccion = request.Direccion?.Trim(),
            SitioWeb = request.SitioWeb?.Trim(),
            Rfc = request.Rfc?.Trim(),
            Rating = request.Rating ?? 5,
            PrecioPromedio = request.PrecioPromedio ?? "$$$",
            Activo = request.Activo ?? true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync(cancellationToken);

        var creado = await GetProveedorByIdAsync(proveedor.Id, cancellationToken);
        return (creado, null);
    }

    public async Task<(ProveedorResponseDto? Result, string? Error)> ActualizarProveedorAsync(int id, ActualizarProveedorRequest request, CancellationToken cancellationToken = default)
    {
        var proveedor = await _db.Proveedores.FindAsync(new object[] { id }, cancellationToken);
        if (proveedor is null)
        {
            return (null, "El proveedor especificado no existe.");
        }

        if (request.CategoriaId > 0 && request.CategoriaId != proveedor.CategoriaId)
        {
            var categoria = await _db.CategoriasProveedores.FindAsync(new object[] { request.CategoriaId }, cancellationToken);
            if (categoria is null)
            {
                return (null, "La categoría especificada no existe.");
            }
            proveedor.CategoriaId = request.CategoriaId;
        }

        if (!string.IsNullOrWhiteSpace(request.Nombre)) proveedor.Nombre = request.Nombre.Trim();
        if (request.Contacto != null) proveedor.Contacto = request.Contacto.Trim();
        if (request.Email != null) proveedor.Email = request.Email.Trim();
        if (request.Telefono != null) proveedor.Telefono = request.Telefono.Trim();
        if (request.Direccion != null) proveedor.Direccion = request.Direccion.Trim();
        if (request.SitioWeb != null) proveedor.SitioWeb = request.SitioWeb.Trim();
        if (request.Rfc != null) proveedor.Rfc = request.Rfc.Trim();
        if (request.Rating.HasValue) proveedor.Rating = request.Rating.Value;
        if (!string.IsNullOrWhiteSpace(request.PrecioPromedio)) proveedor.PrecioPromedio = request.PrecioPromedio;
        if (request.Activo.HasValue) proveedor.Activo = request.Activo.Value;
        proveedor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var actualizado = await GetProveedorByIdAsync(id, cancellationToken);
        return (actualizado, null);
    }

    public async Task<bool> DeleteProveedorAsync(int id, CancellationToken cancellationToken = default)
    {
        var proveedor = await _db.Proveedores.FindAsync(new object[] { id }, cancellationToken);
        if (proveedor is null) return false;

        _db.Proveedores.Remove(proveedor);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Categorías
    public async Task<List<CategoriaProveedorDto>> GetCategoriasAsync(bool incluirInactivos = false, CancellationToken cancellationToken = default)
    {
        var query = _db.CategoriasProveedores.AsQueryable();
        if (!incluirInactivos)
        {
            query = query.Where(c => c.Activo);
        }

        var list = await query.OrderBy(c => c.Nombre).ToListAsync(cancellationToken);
        return list.Select(c => new CategoriaProveedorDto(c.Id, c.Nombre, c.Descripcion, c.Activo)).ToList();
    }

    // Servicios
    public async Task<List<ProveedorServicioDto>> GetServiciosByProveedorIdAsync(int proveedorId, CancellationToken cancellationToken = default)
    {
        var list = await _db.ProveedorServicios
            .Where(ps => ps.ProveedorId == proveedorId && ps.Activo)
            .OrderBy(ps => ps.NombreServicio)
            .ToListAsync(cancellationToken);

        return list.Select(ps => new ProveedorServicioDto(ps.Id, ps.ProveedorId, ps.NombreServicio, ps.Descripcion, ps.Costo, ps.Unidad, ps.Activo)).ToList();
    }

    public async Task<(ProveedorServicioDto? Result, string? Error)> CrearServicioAsync(int proveedorId, CrearProveedorServicioRequest request, CancellationToken cancellationToken = default)
    {
        var proveedor = await _db.Proveedores.FindAsync(new object[] { proveedorId }, cancellationToken);
        if (proveedor is null)
        {
            return (null, "El proveedor no existe.");
        }

        var servicio = new ProveedorServicio
        {
            ProveedorId = proveedorId,
            NombreServicio = request.NombreServicio.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Costo = request.Costo,
            Unidad = request.Unidad ?? "paquete",
            Activo = request.Activo ?? true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ProveedorServicios.Add(servicio);
        await _db.SaveChangesAsync(cancellationToken);

        return (new ProveedorServicioDto(servicio.Id, servicio.ProveedorId, servicio.NombreServicio, servicio.Descripcion, servicio.Costo, servicio.Unidad, servicio.Activo), null);
    }

    public async Task<(ProveedorServicioDto? Result, string? Error)> ActualizarServicioAsync(int id, ActualizarProveedorServicioRequest request, CancellationToken cancellationToken = default)
    {
        var servicio = await _db.ProveedorServicios.FindAsync(new object[] { id }, cancellationToken);
        if (servicio is null)
        {
            return (null, "El servicio de proveedor no existe.");
        }

        if (!string.IsNullOrWhiteSpace(request.NombreServicio)) servicio.NombreServicio = request.NombreServicio.Trim();
        if (request.Descripcion != null) servicio.Descripcion = request.Descripcion.Trim();
        if (request.Costo >= 0) servicio.Costo = request.Costo;
        if (!string.IsNullOrWhiteSpace(request.Unidad)) servicio.Unidad = request.Unidad;
        if (request.Activo.HasValue) servicio.Activo = request.Activo.Value;
        servicio.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (new ProveedorServicioDto(servicio.Id, servicio.ProveedorId, servicio.NombreServicio, servicio.Descripcion, servicio.Costo, servicio.Unidad, servicio.Activo), null);
    }

    public async Task<bool> DeleteServicioAsync(int id, CancellationToken cancellationToken = default)
    {
        var servicio = await _db.ProveedorServicios.FindAsync(new object[] { id }, cancellationToken);
        if (servicio is null) return false;

        _db.ProveedorServicios.Remove(servicio);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProveedorResponseDto MapToResponse(Proveedor p)
    {
        var servicios = p.Servicios?
            .Where(s => s.Activo)
            .Select(s => new ProveedorServicioDto(s.Id, s.ProveedorId, s.NombreServicio, s.Descripcion, s.Costo, s.Unidad, s.Activo))
            .ToList() ?? new List<ProveedorServicioDto>();

        return new ProveedorResponseDto(
            p.Id,
            p.CategoriaId,
            p.Categoria?.Nombre ?? "General",
            p.Nombre,
            p.Contacto,
            p.Email,
            p.Telefono,
            p.Direccion,
            p.SitioWeb,
            p.Rfc,
            p.Rating,
            p.PrecioPromedio,
            p.Activo,
            p.CreatedAt,
            servicios
        );
    }
}
