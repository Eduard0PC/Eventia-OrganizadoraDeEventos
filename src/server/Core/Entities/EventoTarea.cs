using System;

namespace Server.Core.Entities;

public class EventoTarea
{
    public int Id { get; set; }
    public int EventoContratadoId { get; set; }
    public string Titulo { get; set; } = null!;
    public bool Completada { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public EventoContratado EventoContratado { get; set; } = null!;
}

