using Microsoft.EntityFrameworkCore;
using Server.Core.Entities;

namespace Server.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<CatalogoEvento> CatalogoEventos => Set<CatalogoEvento>();
    public DbSet<CatalogoServicio> CatalogoServicios => Set<CatalogoServicio>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<CotizacionItem> CotizacionItems => Set<CotizacionItem>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<EventoContratado> EventosContratados => Set<EventoContratado>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Email).HasColumnName("email");
            e.Property(u => u.PasswordHash).HasColumnName("password_hash");
            e.Property(u => u.Rol).HasColumnName("rol");
            e.Property(u => u.ClienteId).HasColumnName("cliente_id");
            e.Property(u => u.EmpleadoId).HasColumnName("empleado_id");
            e.Property(u => u.Activo).HasColumnName("activo");
            e.Property(u => u.UltimoAcceso).HasColumnName("ultimo_acceso");
            e.HasOne(u => u.Cliente)
                .WithMany()
                .HasForeignKey(u => u.ClienteId);
            e.HasOne(u => u.Empleado)
                .WithMany()
                .HasForeignKey(u => u.EmpleadoId);
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

        modelBuilder.Entity<Empleado>(e =>
        {
            e.ToTable("empleados");
            e.HasKey(em => em.Id);
            e.Property(em => em.Id).HasColumnName("id");
            e.Property(em => em.Nombre).HasColumnName("nombre");
            e.Property(em => em.Apellido).HasColumnName("apellido");
            e.Property(em => em.Email).HasColumnName("email");
            e.Property(em => em.Telefono).HasColumnName("telefono");
            e.Property(em => em.Cargo).HasColumnName("cargo");
            e.Property(em => em.Disponible).HasColumnName("disponible");
        });

        modelBuilder.Entity<CatalogoEvento>(e =>
        {
            e.ToTable("catalogo_eventos");
            e.HasKey(ce => ce.Id);
            e.Property(ce => ce.Id).HasColumnName("id");
            e.Property(ce => ce.Nombre).HasColumnName("nombre");
            e.Property(ce => ce.Descripcion).HasColumnName("descripcion");
            e.Property(ce => ce.PrecioBase).HasColumnName("precio_base");
            e.Property(ce => ce.DuracionHoras).HasColumnName("duracion_horas");
            e.Property(ce => ce.Categoria).HasColumnName("categoria");
            e.Property(ce => ce.CapacidadPersonas).HasColumnName("capacidad_personas");
            e.Property(ce => ce.Activo).HasColumnName("activo");
            e.Property(ce => ce.ImagenUrl).HasColumnName("imagen_url");
            e.Property(ce => ce.CreatedAt).HasColumnName("created_at");
            e.Property(ce => ce.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<CatalogoServicio>(e =>
        {
            e.ToTable("catalogo_servicios");
            e.HasKey(cs => cs.Id);
            e.Property(cs => cs.Id).HasColumnName("id");
            e.Property(cs => cs.Nombre).HasColumnName("nombre");
            e.Property(cs => cs.Descripcion).HasColumnName("descripcion");
            e.Property(cs => cs.PrecioBase).HasColumnName("precio_base");
            e.Property(cs => cs.Unidad).HasColumnName("unidad");
            e.Property(cs => cs.Activo).HasColumnName("activo");
            e.Property(cs => cs.CreatedAt).HasColumnName("created_at");
            e.Property(cs => cs.UpdatedAt).HasColumnName("updated_at");
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
            e.Property(c => c.Estatus).HasColumnName("estatus");
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
            e.Property(i => i.Tipo).HasColumnName("tipo");
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

            e.HasOne(i => i.CatalogoServicio)
                .WithMany()
                .HasForeignKey(i => i.CatalogoServicioId);
        });

        modelBuilder.Entity<Contrato>(e =>
        {
            e.ToTable("contratos");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.ClienteId).HasColumnName("cliente_id");
            e.Property(c => c.CotizacionId).HasColumnName("cotizacion_id");
            e.Property(c => c.Folio).HasColumnName("folio");
            e.Property(c => c.FechaFirma).HasColumnName("fecha_firma");
            e.Property(c => c.FechaInicio).HasColumnName("fecha_inicio");
            e.Property(c => c.FechaFin).HasColumnName("fecha_fin");
            e.Property(c => c.TotalContrato).HasColumnName("total_contrato");
            e.Property(c => c.Estatus).HasColumnName("estatus");
            e.Property(c => c.Condiciones).HasColumnName("condiciones");
            e.Property(c => c.ArchivoUrl).HasColumnName("archivo_url");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");

            e.HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.ClienteId);

            e.HasOne(c => c.Cotizacion)
                .WithMany()
                .HasForeignKey(c => c.CotizacionId);
        });

        modelBuilder.Entity<EventoContratado>(e =>
        {
            e.ToTable("eventos_contratados");
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Id).HasColumnName("id");
            e.Property(ev => ev.ContratoId).HasColumnName("contrato_id");
            e.Property(ev => ev.CatalogoEventoId).HasColumnName("catalogo_evento_id");
            e.Property(ev => ev.FechaEvento).HasColumnName("fecha_evento");
            e.Property(ev => ev.HoraInicio).HasColumnName("hora_inicio");
            e.Property(ev => ev.HoraFin).HasColumnName("hora_fin");
            e.Property(ev => ev.Lugar).HasColumnName("lugar");
            e.Property(ev => ev.Aforo).HasColumnName("aforo");
            e.Property(ev => ev.Estatus).HasColumnName("estatus");
            e.Property(ev => ev.Notas).HasColumnName("notas");
            e.Property(ev => ev.CreatedAt).HasColumnName("created_at");
            e.Property(ev => ev.UpdatedAt).HasColumnName("updated_at");

            e.HasOne(ev => ev.Contrato)
                .WithMany(c => c.EventosContratados)
                .HasForeignKey(ev => ev.ContratoId);

            e.HasOne(ev => ev.CatalogoEvento)
                .WithMany()
                .HasForeignKey(ev => ev.CatalogoEventoId);
        });

        modelBuilder.Entity<Pago>(e =>
        {
            e.ToTable("pagos");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.ContratoId).HasColumnName("contrato_id");
            e.Property(p => p.PlanPagoId).HasColumnName("plan_pago_id");
            e.Property(p => p.Monto).HasColumnName("monto");
            e.Property(p => p.MetodoPago).HasColumnName("metodo_pago");
            e.Property(p => p.TipoTransaccion).HasColumnName("tipo_transaccion");
            e.Property(p => p.Referencia).HasColumnName("referencia");
            e.Property(p => p.FechaPago).HasColumnName("fecha_pago");
            e.Property(p => p.Estatus).HasColumnName("estatus");
            e.Property(p => p.Notas).HasColumnName("notas");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");

            e.HasOne(p => p.Contrato)
                .WithMany(c => c.Pagos)
                .HasForeignKey(p => p.ContratoId);
        });
    }
}
