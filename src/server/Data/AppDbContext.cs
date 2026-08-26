// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CatalogoEvento> CatalogoEventos => Set<CatalogoEvento>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<CotizacionItem> CotizacionItems => Set<CotizacionItem>();

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

        modelBuilder.Entity<Cotizacion>(e =>
        {
            e.ToTable("cotizaciones");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.ClienteId).HasColumnName("cliente_id");
            e.Property(c => c.RecursoId).HasColumnName("recurso_id");
            e.Property(c => c.Folio).HasColumnName("folio");
            e.Property(c => c.Total).HasColumnName("total");
            e.Property(c => c.Descuento).HasColumnName("descuento");
            e.Property(c => c.Estatus).HasColumnName("estatus")
                .HasColumnType("estatus_cotizacion");
            e.Property(c => c.FechaVigencia).HasColumnName("fecha_vigencia");
            e.Property(c => c.FechaEvento).HasColumnName("fecha_evento");
            e.Property(c => c.Invitados).HasColumnName("invitados");
            e.Property(c => c.Notas).HasColumnName("notas");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            e.Property(c => c.TotalFinal).HasColumnName("total_final").ValueGeneratedOnAddOrUpdate();

            e.HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.ClienteId);

            e.HasMany(c => c.Items)
                .WithOne(i => i.Cotizacion)
                .HasForeignKey(i => i.CotizacionId);
        });

        modelBuilder.Entity<CotizacionItem>(e =>
        {
            e.ToTable("cotizacion_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.CotizacionId).HasColumnName("cotizacion_id");
            e.Property(i => i.Tipo).HasColumnName("tipo")
                .HasColumnType("tipo_item_cotizacion");
            e.Property(i => i.Cantidad).HasColumnName("cantidad");
            e.Property(i => i.PrecioUnitario).HasColumnName("precio_unitario");
            e.Property(i => i.DescuentoItem).HasColumnName("descuento_item");
            e.Property(i => i.Notas).HasColumnName("notas");
            e.Property(i => i.CatalogoEventoId).HasColumnName("catalogo_evento_id");
            e.Property(i => i.CatalogoServicioId).HasColumnName("catalogo_servicio_id");
            e.Property(i => i.Subtotal).HasColumnName("subtotal").ValueGeneratedOnAddOrUpdate();
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");

            e.HasOne(i => i.CatalogoEvento)
                .WithMany()
                .HasForeignKey(i => i.CatalogoEventoId);
        });
    }
}
