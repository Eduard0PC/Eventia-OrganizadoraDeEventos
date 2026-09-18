using System;
using System.Collections.Generic;

namespace Server.Core.Entities;

public class Proveedor
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Contacto { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? SitioWeb { get; set; }
    public string? Rfc { get; set; }
    public int Rating { get; set; } = 5;
    public string PrecioPromedio { get; set; } = "$$$";
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CategoriaProveedor Categoria { get; set; } = null!;
    public ICollection<ProveedorServicio> Servicios { get; set; } = new List<ProveedorServicio>();
}
