// Models/CatalogoEvento.cs
using System;

public class CatalogoEvento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal PrecioBase { get; set; }
    public short DuracionHoras { get; set; }
    public bool Activo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
