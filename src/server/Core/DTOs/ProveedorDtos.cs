using System;
using System.Collections.Generic;

namespace Server.Core.DTOs;

public record CategoriaProveedorDto(
    int Id,
    string Nombre,
    string? Descripcion,
    bool Activo
);

public record ProveedorServicioDto(
    int Id,
    int ProveedorId,
    string NombreServicio,
    string? Descripcion,
    decimal Costo,
    string Unidad,
    bool Activo
);

public record ProveedorResponseDto(
    int Id,
    int CategoriaId,
    string CategoriaNombre,
    string Nombre,
    string? Contacto,
    string? Email,
    string? Telefono,
    string? Direccion,
    string? SitioWeb,
    string? Rfc,
    int Rating,
    string PrecioPromedio,
    bool Activo,
    DateTime CreatedAt,
    List<ProveedorServicioDto> Servicios
);

public record CrearProveedorRequest(
    int CategoriaId,
    string Nombre,
    string? Contacto,
    string? Email,
    string? Telefono,
    string? Direccion,
    string? SitioWeb,
    string? Rfc,
    int? Rating = 5,
    string? PrecioPromedio = "$$$",
    bool? Activo = true
);

public record ActualizarProveedorRequest(
    int CategoriaId,
    string Nombre,
    string? Contacto,
    string? Email,
    string? Telefono,
    string? Direccion,
    string? SitioWeb,
    string? Rfc,
    int? Rating,
    string? PrecioPromedio,
    bool? Activo
);

public record CrearProveedorServicioRequest(
    string NombreServicio,
    string? Descripcion,
    decimal Costo,
    string? Unidad = "paquete",
    bool? Activo = true
);

public record ActualizarProveedorServicioRequest(
    string NombreServicio,
    string? Descripcion,
    decimal Costo,
    string? Unidad,
    bool? Activo
);
