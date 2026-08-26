// Models/Cotizacion.cs
using System;
using System.Collections.Generic;

public class Cotizacion
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int? RecursoId { get; set; }
    public string Folio { get; set; } = null!;
    public decimal Total { get; set; }
    public decimal Descuento { get; set; }
    public string Estatus { get; set; } = "enviada"; // borrador, enviada, aceptada, rechazada, vencida
    public DateTime FechaVigencia { get; set; }
    public DateTime? FechaEvento { get; set; }
    public int Invitados { get; set; }
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public decimal? TotalFinal { get; set; }

    public Cliente? Cliente { get; set; }
    public List<CotizacionItem> Items { get; set; } = new();
}
