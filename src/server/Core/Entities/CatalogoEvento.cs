using System;

namespace Server.Core.Entities;

public class CatalogoEvento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal PrecioBase { get; set; }
    public short DuracionHoras { get; set; }
    public string Categoria { get; set; } = "Sociales";
    public int CapacidadPersonas { get; set; } = 50;
    public bool Activo { get; set; }
    public string? ImagenUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
