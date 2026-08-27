using System.Collections.Generic;

namespace Server.Core.DTOs;

public record CotizacionItemDto(int Id, string Descripcion, decimal Monto);

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
    List<CotizacionItemDto> Items
);

public record CrearCotizacionRequest(
    int? ClienteId,
    int CatalogoEventoId,
    string FechaEvento,
    int Invitados,
    int HorasAdicionales,
    string? Notas
);
