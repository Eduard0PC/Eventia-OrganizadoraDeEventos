public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Rol { get; set; } = null!; 
    public int? ClienteId { get; set; }
    public int? EmpleadoId { get; set; }
    public bool Activo { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public Cliente? Cliente { get; set; }
}
