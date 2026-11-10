using System;

namespace Server.Core.Entities;

public class EventoProveedor
{
    public int Id { get; set; }
    public int EventoContratadoId { get; set; }
    public int ProveedorId { get; set; }
    public int? CatalogoServicioId { get; set; }
    public string ClaveRequerimiento { get; set; } = null!;
    public string NombreRequerimiento { get; set; } = null!;
    public string CategoriaProveedor { get; set; } = null!;
    public bool EsLocacion { get; set; }
    public decimal CostoAcordado { get; set; }
    public string? Notas { get; set; }
    public bool Confirmado { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public EventoContratado EventoContratado { get; set; } = null!;
    public Proveedor Proveedor { get; set; } = null!;
    public CatalogoServicio? CatalogoServicio { get; set; }
}

