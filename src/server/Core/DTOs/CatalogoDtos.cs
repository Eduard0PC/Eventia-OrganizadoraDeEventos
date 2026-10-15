namespace Server.Core.DTOs;

public record CrearCatalogoEventoRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    short DuracionHoras,
    string? Categoria = "Sociales",
    int? CapacidadPersonas = 50,
    bool? Activo = true,
    string? ImagenUrl = null
);

public record ActualizarCatalogoEventoRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    short DuracionHoras,
    string? Categoria = null,
    int? CapacidadPersonas = null,
    bool? Activo = null,
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
/// <summary>Servicio extra ofrecido al cliente según la categoría del evento.</summary>
public record ServicioExtraDto(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    string Unidad,
    string? Categoria,
    string? CategoriaProveedor
);
