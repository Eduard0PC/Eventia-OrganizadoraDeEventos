using System;

namespace Server.Core.Entities;

public class ProveedorDisponibilidad
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public DateTime Fecha { get; set; }
    public bool Disponible { get; set; } = true;
    public string? Nota { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Proveedor Proveedor { get; set; } = null!;
}

