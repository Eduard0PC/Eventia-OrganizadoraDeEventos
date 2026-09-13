namespace Server.Core.Entities;

public class Empleado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? Cargo { get; set; }
    public bool Disponible { get; set; }
}
