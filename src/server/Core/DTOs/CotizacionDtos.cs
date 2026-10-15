using System.Collections.Generic;

namespace Server.Core.DTOs;

public record CotizacionItemDto(
    int Id,
    string Tipo,
    string Descripcion,
    decimal PrecioUnitario,
    int Cantidad,
    decimal DescuentoItem,
    decimal Monto,
    string? Notas,
    int? CatalogoServicioId = null,
    // true = servicio extra elegido por el cliente (no es invitado/hora adicional).
    bool EsServicioExtra = false,
    // Tipo de proveedor sugerido para cubrir el servicio (para el planificador).
    string? CategoriaProveedor = null
);

public record CotizacionResponse(
    int Id,
    string Folio,
    string Evento,
    int? CatalogoEventoId,
    string? FechaEvento,
    int Invitados,
    decimal Total,
    decimal Descuento,
    decimal TotalFinal,
    string Estatus,
    string FechaVigencia,
    string FechaCreacion,
    string Notas,
    List<CotizacionItemDto> Items,
    string? CategoriaEvento = null
);

public record CrearCotizacionRequest(
    int? ClienteId,
    int CatalogoEventoId,
    string FechaEvento,
    int Invitados,
    int HorasAdicionales,
    string? Notas,
    string? Estatus,
    // Ids de catalogo_servicios (servicios extra) elegidos por el cliente.
    List<int>? ServiciosExtraIds = null
);
