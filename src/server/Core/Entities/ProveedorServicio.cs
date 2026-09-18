using System;

namespace Server.Core.Entities;

public class ProveedorServicio
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public string NombreServicio { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal Costo { get; set; }
    public string Unidad { get; set; } = "paquete";
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Proveedor Proveedor { get; set; } = null!;
}
