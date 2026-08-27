using System;
using System.Collections.Generic;

namespace Server.Core.Entities;

public class Contrato
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int? CotizacionId { get; set; }
    public string Folio { get; set; } = null!;
    public DateTime? FechaFirma { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal TotalContrato { get; set; }
    public string Estatus { get; set; } = "activo";
    public string? Condiciones { get; set; }
    public string? ArchivoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Cliente Cliente { get; set; } = null!;
    public Cotizacion? Cotizacion { get; set; }
    public ICollection<EventoContratado> EventosContratados { get; set; } = new List<EventoContratado>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
