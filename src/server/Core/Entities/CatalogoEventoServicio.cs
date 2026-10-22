using System;

namespace Server.Core.Entities;

public class CatalogoEventoServicio
{
    public int Id { get; set; }
    public string Categoria { get; set; } = null!;
    public int CatalogoServicioId { get; set; }
    public bool EsObligatorio { get; set; } = true;
    public bool IncluidoEnPrecio { get; set; } = true;
    public short OrdenPrioridad { get; set; } = 1;
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CatalogoServicio? CatalogoServicio { get; set; }
}
