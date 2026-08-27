using System;

namespace Server.Core.Entities;

public class CotizacionItem
{
    public int Id { get; set; }
    public int CotizacionId { get; set; }
    public string Tipo { get; set; } = "evento"; // evento, servicio
    public short Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoItem { get; set; }
    public string? Notas { get; set; }
    public int? CatalogoEventoId { get; set; }
    public int? CatalogoServicioId { get; set; }
    public decimal? Subtotal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Cotizacion? Cotizacion { get; set; }
    public CatalogoEvento? CatalogoEvento { get; set; }
}
