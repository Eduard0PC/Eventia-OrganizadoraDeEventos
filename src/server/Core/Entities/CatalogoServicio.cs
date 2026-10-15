using System;

namespace Server.Core.Entities;

public class CatalogoServicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal PrecioBase { get; set; }
    public string Unidad { get; set; } = "unidad";
    public bool Activo { get; set; } = true;
    /// <summary>Categoría de evento a la que aplica (Bodas, Sociales, Corporativos, Graduaciones). Null = todas.</summary>
    public string? Categoria { get; set; }
    /// <summary>Tipo de proveedor que cubre el servicio.</summary>
    public int? CategoriaProveedorId { get; set; }
    /// <summary>False para servicios internos (invitado/hora adicional) que no se ofrecen al cliente.</summary>
    public bool VisibleCliente { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CategoriaProveedor? CategoriaProveedor { get; set; }
}
