namespace Server.Core.DTOs;

public record CrearCatalogoEventoRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    short DuracionHoras,
    bool? Activo,
    string? ImagenUrl = null
);

public record ActualizarCatalogoEventoRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    short DuracionHoras,
    bool? Activo,
    string? ImagenUrl = null
);

public record CrearCatalogoServicioRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    string? Unidad,
    bool? Activo
);

public record ActualizarCatalogoServicioRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    string? Unidad,
    bool? Activo
);


