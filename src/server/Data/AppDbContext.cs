// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CatalogoEvento> CatalogoEventos => Set<CatalogoEvento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogoEvento>(e =>
        {
            e.ToTable("catalogo_eventos");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Nombre).HasColumnName("nombre");
            e.Property(c => c.Descripcion).HasColumnName("descripcion");
            e.Property(c => c.PrecioBase).HasColumnName("precio_base");
            e.Property(c => c.DuracionHoras).HasColumnName("duracion_horas");
            e.Property(c => c.Activo).HasColumnName("activo");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Email).HasColumnName("email");
            e.Property(u => u.PasswordHash).HasColumnName("password_hash");
            e.Property(u => u.Rol).HasColumnName("rol")
                .HasConversion<string>(); 
            e.Property(u => u.ClienteId).HasColumnName("cliente_id");
            e.Property(u => u.EmpleadoId).HasColumnName("empleado_id");
            e.Property(u => u.Activo).HasColumnName("activo");
            e.Property(u => u.UltimoAcceso).HasColumnName("ultimo_acceso");
            e.HasOne(u => u.Cliente)
                .WithMany()
                .HasForeignKey(u => u.ClienteId);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Nombre).HasColumnName("nombre");
            e.Property(c => c.Apellido).HasColumnName("apellido");
            e.Property(c => c.Email).HasColumnName("email");
            e.Property(c => c.Telefono).HasColumnName("telefono");
            e.Property(c => c.Activo).HasColumnName("activo");
        });
    }
}
