using System;
using System.Collections.Generic;

namespace Server.Core.Entities;

public class PlanPago
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public short NumeroPago { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Estatus { get; set; } = "pendiente"; // pendiente, pagado, vencido, cancelado
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Contrato Contrato { get; set; } = null!;
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
