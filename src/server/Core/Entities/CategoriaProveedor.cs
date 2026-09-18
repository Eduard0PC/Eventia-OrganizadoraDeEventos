using System;
using System.Collections.Generic;

namespace Server.Core.Entities;

public class CategoriaProveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Proveedor> Proveedores { get; set; } = new List<Proveedor>();
}
