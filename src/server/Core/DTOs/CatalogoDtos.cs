namespace Server.Core.DTOs;

public record CrearCatalogoEventoRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    short DuracionHoras,
    bool? Activo
);

public record ActualizarCatalogoEventoRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    short DuracionHoras,
    bool? Activo
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


