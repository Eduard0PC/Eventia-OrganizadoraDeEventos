using System;

namespace Server.Core.Entities;

public class Ticket
{
    public int Id { get; set; }
    public int PagoId { get; set; }
    public string Folio { get; set; } = null!;
    public DateTime EmitidoAt { get; set; } = DateTime.UtcNow;
    public bool Impreso { get; set; } = false;
    public string? UrlPdf { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Pago Pago { get; set; } = null!;
}
