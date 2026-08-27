using System;

namespace Server.Core.Entities;

public class EventoContratado
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public int CatalogoEventoId { get; set; }
    public DateTime FechaEvento { get; set; }
    public TimeSpan? HoraInicio { get; set; }
    public TimeSpan? HoraFin { get; set; }
    public string? Lugar { get; set; }
    public short? Aforo { get; set; }
    public string Estatus { get; set; } = "programado";
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Contrato Contrato { get; set; } = null!;
    public CatalogoEvento CatalogoEvento { get; set; } = null!;
}
