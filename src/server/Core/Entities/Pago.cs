using System;

namespace Server.Core.Entities;

public class Pago
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public int? PlanPagoId { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "efectivo";
    public string TipoTransaccion { get; set; } = "parcial";
    public string? Referencia { get; set; }
    public DateTime FechaPago { get; set; }
    public string Estatus { get; set; } = "procesado";
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Contrato Contrato { get; set; } = null!;
}
