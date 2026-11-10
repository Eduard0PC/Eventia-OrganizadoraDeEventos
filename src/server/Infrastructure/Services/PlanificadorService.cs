using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Core.DTOs;
using Server.Core.Entities;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Infrastructure.Services;

public class PlanificadorService : IPlanificadorService
{
    public const string ClaveLocacion = "LOCACION";

    private static bool _esquemaVerificado;
    private readonly AppDbContext _db;

    public PlanificadorService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PlanificacionEventoDto>> GetPlanificacionesAsync(CancellationToken cancellationToken = default)
    {
        await AsegurarTablasPlanificadorAsync(cancellationToken);
        await SincronizarContratosSinEventoAsync(cancellationToken);

        var eventos = await CargarEventosQuery()
            .OrderBy(ev => ev.FechaEvento)
            .ThenByDescending(ev => ev.CreatedAt)
            .ToListAsync(cancellationToken);

        var serviciosDefecto = await CargarServiciosDefectoAsync(cancellationToken);

        return eventos
            .Select(ev => MapToPlanificacionDto(ev, serviciosDefecto))
            .ToList();
    }

    public async Task<PlanificacionEventoDto?> GetPlanificacionByEventoIdAsync(int eventoContratadoId, CancellationToken cancellationToken = default)
    {
        var ev = await CargarEventosQuery()
            .FirstOrDefaultAsync(e => e.Id == eventoContratadoId, cancellationToken);

        if (ev is null) return null;

        var serviciosDefecto = await CargarServiciosDefectoAsync(cancellationToken);

        return MapToPlanificacionDto(ev, serviciosDefecto);
    }

    public async Task<List<ProveedorDisponibleDto>> GetProveedoresDisponiblesParaEventoAsync(
        int eventoContratadoId,
        string? categoriaProveedor,
        bool esLocacion = false,
        CancellationToken cancellationToken = default)
    {
        var ev = await _db.EventosContratados
            .AsNoTracking()
            .Include(e => e.CatalogoEvento)
            .FirstOrDefaultAsync(e => e.Id == eventoContratadoId, cancellationToken);

        if (ev is null) return new List<ProveedorDisponibleDto>();

        DateTime fechaEvento = ev.FechaEvento.Date;
        int aforoRequerido = ev.Aforo ?? ev.CatalogoEvento?.CapacidadPersonas ?? 50;

        // 1. Obtener IDs de proveedores ya bloqueados/asignados en cualquier evento activo en esa misma fecha
        var ocupadosPorAsignacion = await _db.EventoProveedores
            .AsNoTracking()
            .Include(ep => ep.EventoContratado)
            .Where(ep =>
                ep.EventoContratado.FechaEvento.Date == fechaEvento &&
                ep.EventoContratado.Estatus != "cancelado")
            .Select(ep => ep.ProveedorId)
            .ToListAsync(cancellationToken);

        var ocupadosPorDisponibilidad = await _db.ProveedorDisponibilidades
            .AsNoTracking()
            .Where(pd => pd.Fecha.Date == fechaEvento && !pd.Disponible)
            .Select(pd => pd.ProveedorId)
            .ToListAsync(cancellationToken);

        var ocupadosSet = ocupadosPorAsignacion
            .Concat(ocupadosPorDisponibilidad)
            .ToHashSet();

        // 2. Consultar proveedores activos que NO estén ocupados en esa fecha
        var proveedoresActivos = await _db.Proveedores
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.Servicios)
            .Where(p => p.Activo && !ocupadosSet.Contains(p.Id))
            .OrderByDescending(p => p.Rating)
            .ThenBy(p => p.Nombre)
            .ToListAsync(cancellationToken);

        bool filtroLocacion = esLocacion || EsCategoriaLocacion(categoriaProveedor);

        var disponibles = proveedoresActivos
            .Where(p =>
            {
                string catProvNombre = p.Categoria?.Nombre ?? "General";

                if (filtroLocacion)
                {
                    if (!EsCategoriaLocacion(catProvNombre)) return false;
                    int capacidadSalon = p.CapacidadMaxima ?? 100;
                    return capacidadSalon >= aforoRequerido;
                }

                if (!string.IsNullOrWhiteSpace(categoriaProveedor))
                {
                    return CategoriasCoinciden(catProvNombre, categoriaProveedor);
                }

                return true;
            })
            .Select(p =>
            {
                var serviciosDto = (p.Servicios ?? Enumerable.Empty<ProveedorServicio>())
                    .Where(s => s.Activo)
                    .OrderBy(s => s.Costo)
                    .Select(s => new ProveedorServicioDto(
                        s.Id,
                        s.ProveedorId,
                        s.NombreServicio,
                        s.Descripcion,
                        s.Costo,
                        s.Unidad,
                        s.Activo))
                    .ToList();

                decimal costoSugerido = serviciosDto.FirstOrDefault()?.Costo
                    ?? EstimarCostoPorRango(p.PrecioPromedio, filtroLocacion);

                return new ProveedorDisponibleDto(
                    p.Id,
                    p.CategoriaId,
                    p.Categoria?.Nombre ?? "General",
                    p.Nombre,
                    p.Contacto,
                    p.Telefono,
                    p.Email,
                    p.Direccion,
                    p.Rating,
                    p.PrecioPromedio,
                    p.CapacidadMaxima,
                    costoSugerido,
                    serviciosDto
                );
            })
            .ToList();

        return disponibles;
    }

    public async Task<(PlanificacionEventoDto? Result, string? Error)> AsignarProveedorAsync(
        int eventoContratadoId,
        AsignarProveedorEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClaveRequerimiento))
        {
            return (null, "La clave del requerimiento es obligatoria.");
        }

        if (request.CostoAcordado < 0)
        {
            return (null, "El costo acordado con el proveedor no puede ser negativo.");
        }

        var ev = await _db.EventosContratados
            .Include(e => e.Contrato)
            .Include(e => e.CatalogoEvento)
            .Include(e => e.ProveedoresAsignados)
            .FirstOrDefaultAsync(e => e.Id == eventoContratadoId, cancellationToken);

        if (ev is null)
        {
            return (null, "El evento contratado no existe.");
        }

        if (ev.Estatus == "cancelado")
        {
            return (null, "No se pueden asignar proveedores a un evento cancelado.");
        }

        var proveedor = await _db.Proveedores
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == request.ProveedorId && p.Activo, cancellationToken);

        if (proveedor is null)
        {
            return (null, "El proveedor seleccionado no existe o se encuentra inactivo.");
        }

        string claveNorm = request.ClaveRequerimiento.Trim();
        bool esLocacion = request.EsLocacion
            || claveNorm.Equals(ClaveLocacion, StringComparison.OrdinalIgnoreCase)
            || EsCategoriaLocacion(request.CategoriaProveedor)
            || EsCategoriaLocacion(proveedor.Categoria?.Nombre);

        // 1. Validar capacidad cuando es Locación / Salón
        if (esLocacion)
        {
            int aforoEvento = ev.Aforo ?? ev.CatalogoEvento?.CapacidadPersonas ?? 50;
            int capacidadSalon = proveedor.CapacidadMaxima ?? 100;

            if (capacidadSalon < aforoEvento)
            {
                return (null, $"La locación \"{proveedor.Nombre}\" tiene capacidad máxima para {capacidadSalon} personas y el evento requiere {aforoEvento} invitados.");
            }
        }

        DateTime fechaEvento = ev.FechaEvento.Date;

        // 2. Validar bloqueo de fecha: el proveedor no puede tener otro evento o requerimiento en la misma fecha
        bool yaOcupadoEnFecha = await _db.EventoProveedores
            .Include(ep => ep.EventoContratado)
            .AnyAsync(ep =>
                ep.ProveedorId == request.ProveedorId &&
                ep.EventoContratado.FechaEvento.Date == fechaEvento &&
                ep.EventoContratado.Estatus != "cancelado" &&
                !(ep.EventoContratadoId == eventoContratadoId && ep.ClaveRequerimiento == claveNorm),
                cancellationToken);

        if (yaOcupadoEnFecha)
        {
            return (null, $"El proveedor \"{proveedor.Nombre}\" ya se encuentra asignado y bloqueado para la fecha {fechaEvento:yyyy-MM-dd}.");
        }

        var asignacionExistente = ev.ProveedoresAsignados
            .FirstOrDefault(ep => ep.ClaveRequerimiento.Equals(claveNorm, StringComparison.OrdinalIgnoreCase));

        bool bloqueadoManual = await _db.ProveedorDisponibilidades
            .AnyAsync(pd =>
                pd.ProveedorId == request.ProveedorId &&
                pd.Fecha.Date == fechaEvento &&
                !pd.Disponible &&
                (asignacionExistente == null || asignacionExistente.ProveedorId != request.ProveedorId),
                cancellationToken);

        if (bloqueadoManual)
        {
            return (null, $"El proveedor \"{proveedor.Nombre}\" no está disponible en la fecha {fechaEvento:yyyy-MM-dd}.");
        }

        // Si había otro proveedor distinto en este mismo requerimiento, liberar su bloqueo de fecha
        if (asignacionExistente != null && asignacionExistente.ProveedorId != request.ProveedorId)
        {
            await LiberarDisponibilidadProveedorEnFechaAsync(asignacionExistente.ProveedorId, fechaEvento, asignacionExistente.Id, cancellationToken);
        }

        if (asignacionExistente is null)
        {
            asignacionExistente = new EventoProveedor
            {
                EventoContratadoId = eventoContratadoId,
                ProveedorId = request.ProveedorId,
                CatalogoServicioId = request.CatalogoServicioId,
                ClaveRequerimiento = claveNorm,
                NombreRequerimiento = !string.IsNullOrWhiteSpace(request.NombreRequerimiento)
                    ? request.NombreRequerimiento.Trim()
                    : (esLocacion ? "Locación / Salón del Evento" : "Servicio de Evento"),
                CategoriaProveedor = !string.IsNullOrWhiteSpace(request.CategoriaProveedor)
                    ? request.CategoriaProveedor.Trim()
                    : (proveedor.Categoria?.Nombre ?? "General"),
                EsLocacion = esLocacion,
                CostoAcordado = request.CostoAcordado,
                Notas = request.Notas?.Trim(),
                Confirmado = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.EventoProveedores.Add(asignacionExistente);
        }
        else
        {
            asignacionExistente.ProveedorId = request.ProveedorId;
            asignacionExistente.CatalogoServicioId = request.CatalogoServicioId;
            asignacionExistente.NombreRequerimiento = !string.IsNullOrWhiteSpace(request.NombreRequerimiento)
                ? request.NombreRequerimiento.Trim()
                : asignacionExistente.NombreRequerimiento;
            asignacionExistente.CategoriaProveedor = !string.IsNullOrWhiteSpace(request.CategoriaProveedor)
                ? request.CategoriaProveedor.Trim()
                : asignacionExistente.CategoriaProveedor;
            asignacionExistente.EsLocacion = esLocacion;
            asignacionExistente.CostoAcordado = request.CostoAcordado;
            asignacionExistente.Notas = request.Notas?.Trim();
            asignacionExistente.Confirmado = true;
            asignacionExistente.UpdatedAt = DateTime.UtcNow;
        }

        // 3. Registrar el bloqueo explícito del proveedor en proveedor_disponibilidad para esa fecha
        await BloquearDisponibilidadProveedorEnFechaAsync(
            request.ProveedorId,
            fechaEvento,
            $"Bloqueado para evento #{ev.Id} ({ev.Contrato?.Folio ?? "Contrato"})",
            cancellationToken);

        // 4. Si es la locación del evento, sincronizar el campo Lugar del EventoContratado
        if (esLocacion)
        {
            ev.Lugar = proveedor.Nombre;
            ev.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await SincronizarEstatusAutomaticoEventoAsync(eventoContratadoId, cancellationToken);

        var actualizado = await GetPlanificacionByEventoIdAsync(eventoContratadoId, cancellationToken);
        return (actualizado, null);
    }

    public async Task<(PlanificacionEventoDto? Result, string? Error)> RemoverAsignacionProveedorAsync(
        int eventoContratadoId,
        int asignacionId,
        CancellationToken cancellationToken = default)
    {
        var asignacion = await _db.EventoProveedores
            .Include(ep => ep.EventoContratado)
            .FirstOrDefaultAsync(ep => ep.Id == asignacionId && ep.EventoContratadoId == eventoContratadoId, cancellationToken);

        if (asignacion is null)
        {
            return (null, "La asignación de proveedor especificada no existe.");
        }

        int proveedorId = asignacion.ProveedorId;
        DateTime fechaEvento = asignacion.EventoContratado.FechaEvento.Date;
        bool eraLocacion = asignacion.EsLocacion || asignacion.ClaveRequerimiento.Equals(ClaveLocacion, StringComparison.OrdinalIgnoreCase);

        _db.EventoProveedores.Remove(asignacion);

        if (eraLocacion)
        {
            asignacion.EventoContratado.Lugar = "Por confirmar con cliente";
            asignacion.EventoContratado.UpdatedAt = DateTime.UtcNow;
        }

        await LiberarDisponibilidadProveedorEnFechaAsync(proveedorId, fechaEvento, asignacion.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await SincronizarEstatusAutomaticoEventoAsync(eventoContratadoId, cancellationToken);

        var actualizado = await GetPlanificacionByEventoIdAsync(eventoContratadoId, cancellationToken);
        return (actualizado, null);
    }

    public async Task<(EventoTareaDto? Result, string? Error)> CrearTareaAsync(
        int eventoContratadoId,
        CrearEventoTareaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            return (null, "El título de la tarea no puede estar vacío.");
        }

        bool existeEvento = await _db.EventosContratados.AnyAsync(e => e.Id == eventoContratadoId, cancellationToken);
        if (!existeEvento)
        {
            return (null, "El evento contratado especificado no existe.");
        }

        var tarea = new EventoTarea
        {
            EventoContratadoId = eventoContratadoId,
            Titulo = request.Titulo.Trim(),
            Completada = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.EventoTareas.Add(tarea);
        await _db.SaveChangesAsync(cancellationToken);
        await SincronizarEstatusAutomaticoEventoAsync(eventoContratadoId, cancellationToken);

        return (new EventoTareaDto(
            tarea.Id,
            tarea.EventoContratadoId,
            tarea.Titulo,
            tarea.Completada,
            tarea.CreatedAt.ToString("yyyy-MM-dd")), null);
    }

    public async Task<(EventoTareaDto? Result, string? Error)> ToggleTareaAsync(
        int eventoContratadoId,
        int tareaId,
        CancellationToken cancellationToken = default)
    {
        var tarea = await _db.EventoTareas
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.EventoContratadoId == eventoContratadoId, cancellationToken);

        if (tarea is null)
        {
            return (null, "La tarea especificada no existe.");
        }

        tarea.Completada = !tarea.Completada;
        tarea.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await SincronizarEstatusAutomaticoEventoAsync(eventoContratadoId, cancellationToken);

        return (new EventoTareaDto(
            tarea.Id,
            tarea.EventoContratadoId,
            tarea.Titulo,
            tarea.Completada,
            tarea.CreatedAt.ToString("yyyy-MM-dd")), null);
    }

    public async Task<bool> EliminarTareaAsync(
        int eventoContratadoId,
        int tareaId,
        CancellationToken cancellationToken = default)
    {
        var tarea = await _db.EventoTareas
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.EventoContratadoId == eventoContratadoId, cancellationToken);

        if (tarea is null) return false;

        _db.EventoTareas.Remove(tarea);
        await _db.SaveChangesAsync(cancellationToken);
        await SincronizarEstatusAutomaticoEventoAsync(eventoContratadoId, cancellationToken);
        return true;
    }

    // =========================================================================
    // HELPERS PRIVADOS
    // =========================================================================

    private IQueryable<EventoContratado> CargarEventosQuery()
    {
        return _db.EventosContratados
            .AsSplitQuery()
            .Include(ev => ev.CatalogoEvento)
            .Include(ev => ev.Contrato)
                .ThenInclude(c => c.Cliente)
            .Include(ev => ev.Contrato)
                .ThenInclude(c => c.Cotizacion)
                    .ThenInclude(cot => cot!.Items)
                        .ThenInclude(i => i.CatalogoServicio)
                            .ThenInclude(cs => cs!.CategoriaProveedor)
            .Include(ev => ev.ProveedoresAsignados)
                .ThenInclude(ep => ep.Proveedor)
                    .ThenInclude(p => p.Categoria)
            .Include(ev => ev.Tareas);
    }

    private async Task AsegurarTablasPlanificadorAsync(CancellationToken cancellationToken)
    {
        if (_esquemaVerificado || !_db.Database.IsRelational()) return;

        try
        {
            await _db.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE public.proveedores
                  ADD COLUMN IF NOT EXISTS capacidad_maxima INTEGER NULL;

                CREATE TABLE IF NOT EXISTS public.evento_proveedores (
                    id SERIAL PRIMARY KEY,
                    evento_contratado_id INTEGER NOT NULL REFERENCES public.eventos_contratados(id) ON DELETE CASCADE,
                    proveedor_id INTEGER NOT NULL REFERENCES public.proveedores(id) ON DELETE RESTRICT,
                    catalogo_servicio_id INTEGER NULL REFERENCES public.catalogo_servicios(id) ON DELETE SET NULL,
                    clave_requerimiento VARCHAR(100) NOT NULL,
                    nombre_requerimiento VARCHAR(200) NOT NULL,
                    categoria_proveedor VARCHAR(100) NOT NULL,
                    es_locacion BOOLEAN NOT NULL DEFAULT FALSE,
                    costo_acordado NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
                    notas TEXT NULL,
                    confirmado BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    CONSTRAINT uq_evento_requerimiento UNIQUE (evento_contratado_id, clave_requerimiento),
                    CONSTRAINT chk_costo_acordado_no_negativo CHECK (costo_acordado >= 0)
                );

                CREATE INDEX IF NOT EXISTS idx_evento_proveedores_evento ON public.evento_proveedores(evento_contratado_id);
                CREATE INDEX IF NOT EXISTS idx_evento_proveedores_proveedor ON public.evento_proveedores(proveedor_id);

                CREATE TABLE IF NOT EXISTS public.evento_tareas (
                    id SERIAL PRIMARY KEY,
                    evento_contratado_id INTEGER NOT NULL REFERENCES public.eventos_contratados(id) ON DELETE CASCADE,
                    titulo VARCHAR(250) NOT NULL,
                    completada BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                );

                CREATE INDEX IF NOT EXISTS idx_evento_tareas_evento ON public.evento_tareas(evento_contratado_id);
            ", cancellationToken);

            _esquemaVerificado = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlanificadorService.AsegurarTablasPlanificadorAsync]: {ex.Message}");
        }
    }

    private async Task<List<CatalogoEventoServicio>> CargarServiciosDefectoAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _db.CatalogoEventoServicios
                .AsNoTracking()
                .Include(ces => ces.CatalogoServicio)
                    .ThenInclude(cs => cs!.CategoriaProveedor)
                .OrderBy(ces => ces.OrdenPrioridad)
                .ToListAsync(cancellationToken);
        }
        catch
        {
            return new List<CatalogoEventoServicio>();
        }
    }

    private async Task SincronizarContratosSinEventoAsync(CancellationToken cancellationToken)
    {
        try
        {
            var contratosSinEvento = await _db.Contratos
                .Include(c => c.Cotizacion)
                    .ThenInclude(cot => cot!.Items)
                .Where(c => c.Estatus != "cancelado" && !c.EventosContratados.Any())
                .ToListAsync(cancellationToken);

            if (contratosSinEvento.Count == 0) return;

            int defaultCatalogoId = await _db.CatalogoEventos
                .Where(e => e.Activo)
                .Select(e => e.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultCatalogoId == 0)
            {
                defaultCatalogoId = await _db.CatalogoEventos
                    .Select(e => e.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (defaultCatalogoId == 0) return;

            foreach (var contrato in contratosSinEvento)
            {
                int catEventoId = contrato.Cotizacion?.Items
                    .FirstOrDefault(i => i.CatalogoEventoId.HasValue)?.CatalogoEventoId
                    ?? defaultCatalogoId;

                var fechaEv = DateTime.SpecifyKind(
                    (contrato.Cotizacion?.FechaEvento ?? contrato.FechaInicio).Date,
                    DateTimeKind.Utc);

                _db.EventosContratados.Add(new EventoContratado
                {
                    ContratoId = contrato.Id,
                    CatalogoEventoId = catEventoId,
                    FechaEvento = fechaEv,
                    Lugar = "Por confirmar con cliente",
                    Aforo = (short?)(contrato.Cotizacion?.Invitados ?? 50),
                    Estatus = contrato.Estatus == "completado" ? "completado" : "programado",
                    Notas = contrato.Condiciones ?? contrato.Cotizacion?.Notas,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // No bloquear la lectura si falla la sincronización automática
        }
    }

    private PlanificacionEventoDto MapToPlanificacionDto(
        EventoContratado ev,
        List<CatalogoEventoServicio> todosServiciosDefecto)
    {
        string clienteNombre = ev.Contrato?.Cliente != null
            ? $"{ev.Contrato.Cliente.Nombre} {ev.Contrato.Cliente.Apellido}".Trim()
            : "Cliente Contratante";

        string categoriaEvento = ev.CatalogoEvento?.Categoria ?? "Sociales";
        int aforo = ev.Aforo ?? ev.Contrato?.Cotizacion?.Invitados ?? ev.CatalogoEvento?.CapacidadPersonas ?? 50;
        decimal totalContrato = ev.Contrato?.TotalContrato ?? ev.CatalogoEvento?.PrecioBase ?? 0m;

        var asignacionesPorClave = (ev.ProveedoresAsignados ?? new List<EventoProveedor>())
            .ToDictionary(ep => ep.ClaveRequerimiento, ep => ep, StringComparer.OrdinalIgnoreCase);

        var requerimientos = new List<RequerimientoProveedorDto>();
        var clavesAgregadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Requerimiento obligatorio #1: Locación / Salón según aforo de personas
        var itemsCotizacion = ev.Contrato?.Cotizacion?.Items ?? new List<CotizacionItem>();
        var itemLocacionCotizado = itemsCotizacion.FirstOrDefault(i =>
            i.Tipo == "servicio" &&
            i.CatalogoServicio != null &&
            EsCategoriaLocacion(i.CatalogoServicio.CategoriaProveedor?.Nombre));

        asignacionesPorClave.TryGetValue(ClaveLocacion, out var asigLocacion);
        if (asigLocacion == null)
        {
            asigLocacion = (ev.ProveedoresAsignados ?? new List<EventoProveedor>())
                .FirstOrDefault(ep => ep.EsLocacion || EsCategoriaLocacion(ep.CategoriaProveedor));
        }

        decimal precioLocacionCliente = itemLocacionCotizado != null
            ? (itemLocacionCotizado.Subtotal ?? (itemLocacionCotizado.Cantidad * itemLocacionCotizado.PrecioUnitario - itemLocacionCotizado.DescuentoItem))
            : 0m;

        requerimientos.Add(new RequerimientoProveedorDto(
            ClaveLocacion,
            itemLocacionCotizado?.CatalogoServicio?.Nombre ?? "Locación / Salón para el Evento",
            $"Espacio requerido con capacidad mínima para {aforo} invitados.",
            "Locación",
            "locacion",
            true,
            aforo,
            itemLocacionCotizado?.CatalogoServicioId,
            precioLocacionCliente,
            asigLocacion != null ? MapAsignacionDto(asigLocacion) : null
        ));
        clavesAgregadas.Add(ClaveLocacion);
        if (asigLocacion != null) clavesAgregadas.Add(asigLocacion.ClaveRequerimiento);

        // 2. Servicios incluidos por defecto en el paquete de la categoría
        var serviciosCategoria = todosServiciosDefecto
            .Where(ces => ces.Categoria.Equals(categoriaEvento, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var def in serviciosCategoria)
        {
            string catProv = def.CatalogoServicio?.CategoriaProveedor?.Nombre ?? "General";
            if (EsCategoriaLocacion(catProv)) continue;

            string clave = $"SRV-{def.CatalogoServicioId}";
            if (clavesAgregadas.Contains(clave)) continue;

            asignacionesPorClave.TryGetValue(clave, out var asig);
            requerimientos.Add(new RequerimientoProveedorDto(
                clave,
                def.CatalogoServicio?.Nombre ?? "Servicio de Paquete",
                def.CatalogoServicio?.Descripcion ?? def.Notas,
                catProv,
                "paquete",
                false,
                null,
                def.CatalogoServicioId,
                0m,
                asig != null ? MapAsignacionDto(asig) : null
            ));
            clavesAgregadas.Add(clave);
        }

        // Si el paquete no tenía servicios por defecto configurados en BD, asegurar rubros base esenciales del paquete
        if (serviciosCategoria.Count == 0)
        {
            var rubrosBase = ObtenerRubrosBasePorCategoria(categoriaEvento);
            foreach (var rubro in rubrosBase)
            {
                if (clavesAgregadas.Contains(rubro.Clave)) continue;

                asignacionesPorClave.TryGetValue(rubro.Clave, out var asig);
                requerimientos.Add(new RequerimientoProveedorDto(
                    rubro.Clave,
                    rubro.Nombre,
                    rubro.Descripcion,
                    rubro.CategoriaProveedor,
                    "paquete",
                    false,
                    null,
                    null,
                    0m,
                    asig != null ? MapAsignacionDto(asig) : null
                ));
                clavesAgregadas.Add(rubro.Clave);
            }
        }

        // 3. Servicios extra contratados por el cliente en la cotización
        foreach (var item in itemsCotizacion.Where(i => i.Tipo == "servicio"))
        {
            if (item.CatalogoServicio != null && !item.CatalogoServicio.VisibleCliente)
            {
                // Ignorar cargos internos ("Invitado adicional", "Hora adicional")
                continue;
            }

            string catProv = item.CatalogoServicio?.CategoriaProveedor?.Nombre ?? "General";
            if (EsCategoriaLocacion(catProv)) continue;

            string clave = item.CatalogoServicioId.HasValue
                ? $"SRV-{item.CatalogoServicioId.Value}"
                : $"ITEM-{item.Id}";

            if (clavesAgregadas.Contains(clave)) continue;

            decimal subtotalItem = item.Subtotal ?? (item.Cantidad * item.PrecioUnitario - item.DescuentoItem);
            asignacionesPorClave.TryGetValue(clave, out var asig);

            requerimientos.Add(new RequerimientoProveedorDto(
                clave,
                item.CatalogoServicio?.Nombre ?? item.Notas ?? "Servicio Adicional Contratado",
                item.CatalogoServicio?.Descripcion,
                catProv,
                "extra",
                false,
                null,
                item.CatalogoServicioId,
                subtotalItem,
                asig != null ? MapAsignacionDto(asig) : null
            ));
            clavesAgregadas.Add(clave);
        }

        // 4. Cualquier otra asignación guardada manualmente para este evento
        foreach (var asigExtra in ev.ProveedoresAsignados ?? new List<EventoProveedor>())
        {
            if (clavesAgregadas.Contains(asigExtra.ClaveRequerimiento)) continue;

            requerimientos.Add(new RequerimientoProveedorDto(
                asigExtra.ClaveRequerimiento,
                asigExtra.NombreRequerimiento,
                asigExtra.Notas,
                asigExtra.CategoriaProveedor,
                "adicional",
                asigExtra.EsLocacion,
                asigExtra.EsLocacion ? aforo : null,
                asigExtra.CatalogoServicioId,
                0m,
                MapAsignacionDto(asigExtra)
            ));
            clavesAgregadas.Add(asigExtra.ClaveRequerimiento);
        }

        var checklistDto = (ev.Tareas ?? new List<EventoTarea>())
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .Select(t => new EventoTareaDto(
                t.Id,
                t.EventoContratadoId,
                t.Titulo,
                t.Completada,
                t.CreatedAt.ToString("yyyy-MM-dd")))
            .ToList();

        int totalReq = requerimientos.Count;
        int reqCubiertos = requerimientos.Count(r => r.Asignacion != null);
        int totalTareas = checklistDto.Count;
        int tareasCompletadas = checklistDto.Count(t => t.Completada);

        var (estatusAutomatico, progreso) = CalcularEstatusYProgresoAutomatico(
            ev.FechaEvento,
            ev.Estatus,
            totalReq,
            reqCubiertos,
            totalTareas,
            tareasCompletadas);

        decimal costoProveedores = (ev.ProveedoresAsignados ?? new List<EventoProveedor>())
            .Sum(ep => ep.CostoAcordado);

        decimal margenGanancia = totalContrato - costoProveedores;
        decimal porcentajeMargen = totalContrato > 0
            ? Math.Round((margenGanancia / totalContrato) * 100m, 1)
            : 0m;

        string lugarMostrado = asigLocacion?.Proveedor?.Nombre
            ?? (!string.IsNullOrWhiteSpace(ev.Lugar) ? ev.Lugar : "Por asignar en planificador");

        return new PlanificacionEventoDto(
            ev.Id,
            ev.ContratoId,
            ev.Contrato?.Folio ?? $"CTR-{ev.ContratoId}",
            ev.CatalogoEvento?.Nombre ?? "Evento Contratado",
            categoriaEvento,
            ev.Contrato?.ClienteId ?? 0,
            clienteNombre,
            ev.Contrato?.Cliente?.Telefono,
            ev.FechaEvento.ToString("yyyy-MM-dd"),
            ev.HoraInicio?.ToString(@"hh\:mm") ?? "18:00",
            ev.HoraFin?.ToString(@"hh\:mm") ?? "23:00",
            lugarMostrado,
            aforo,
            estatusAutomatico,
            ev.Notas ?? ev.Contrato?.Condiciones ?? ev.Contrato?.Cotizacion?.Notas,
            totalContrato,
            costoProveedores,
            margenGanancia,
            porcentajeMargen,
            totalReq,
            reqCubiertos,
            progreso,
            requerimientos,
            checklistDto
        );
    }

    private async Task SincronizarEstatusAutomaticoEventoAsync(int eventoContratadoId, CancellationToken cancellationToken)
    {
        var ev = await CargarEventosQuery()
            .FirstOrDefaultAsync(e => e.Id == eventoContratadoId, cancellationToken);

        if (ev is null || ev.Estatus == "cancelado") return;

        var serviciosDefecto = await CargarServiciosDefectoAsync(cancellationToken);
        var dto = MapToPlanificacionDto(ev, serviciosDefecto);

        if (!string.Equals(ev.Estatus, dto.EstatusEvento, StringComparison.OrdinalIgnoreCase))
        {
            ev.Estatus = dto.EstatusEvento;
            ev.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static (string Estatus, int Progreso) CalcularEstatusYProgresoAutomatico(
        DateTime fechaEvento,
        string? estatusActual,
        int totalReq,
        int reqCubiertos,
        int totalTareas,
        int tareasCompletadas)
    {
        if (string.Equals(estatusActual, "cancelado", StringComparison.OrdinalIgnoreCase))
        {
            return ("cancelado", 0);
        }

        int totalItemsAvance = totalReq + totalTareas;
        int itemsCompletados = reqCubiertos + tareasCompletadas;
        int progreso = totalItemsAvance > 0
            ? (int)Math.Round((itemsCompletados * 100.0) / totalItemsAvance)
            : 0;

        bool todosReqCubiertos = totalReq > 0 && reqCubiertos >= totalReq;
        bool todasTareasCompletadas = totalTareas > 0 && tareasCompletadas >= totalTareas;
        bool hayTareasCompletadas = tareasCompletadas > 0;

        DateTime hoy = DateTime.UtcNow.Date;
        DateTime fechaEv = fechaEvento.Date;

        string estatusCalculado;
        if (todosReqCubiertos && (todasTareasCompletadas || fechaEv < hoy))
        {
            estatusCalculado = "completado";
            progreso = 100;
        }
        else if (fechaEv == hoy || (todosReqCubiertos && hayTareasCompletadas) || (reqCubiertos > 0 && hayTareasCompletadas))
        {
            estatusCalculado = "en_curso";
        }
        else
        {
            estatusCalculado = "programado";
        }

        return (estatusCalculado, progreso);
    }

    private static ProveedorAsignadoDto MapAsignacionDto(EventoProveedor ep)
    {
        return new ProveedorAsignadoDto(
            ep.Id,
            ep.ProveedorId,
            ep.Proveedor?.Nombre ?? "Proveedor Asignado",
            ep.Proveedor?.Contacto,
            ep.Proveedor?.Telefono,
            ep.Proveedor?.Email,
            ep.Proveedor?.Direccion,
            ep.Proveedor?.Rating ?? 5,
            ep.Proveedor?.PrecioPromedio ?? "$$$",
            ep.Proveedor?.CapacidadMaxima,
            ep.CostoAcordado,
            ep.Notas,
            ep.Confirmado,
            ep.UpdatedAt.ToString("yyyy-MM-dd")
        );
    }

    private async Task BloquearDisponibilidadProveedorEnFechaAsync(
        int proveedorId,
        DateTime fecha,
        string nota,
        CancellationToken cancellationToken)
    {
        var disp = await _db.ProveedorDisponibilidades
            .FirstOrDefaultAsync(pd => pd.ProveedorId == proveedorId && pd.Fecha.Date == fecha.Date, cancellationToken);

        if (disp is null)
        {
            _db.ProveedorDisponibilidades.Add(new ProveedorDisponibilidad
            {
                ProveedorId = proveedorId,
                Fecha = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc),
                Disponible = false,
                Nota = nota,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            disp.Disponible = false;
            disp.Nota = nota;
            disp.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task LiberarDisponibilidadProveedorEnFechaAsync(
        int proveedorId,
        DateTime fecha,
        int asignacionIdExcluida,
        CancellationToken cancellationToken)
    {
        bool tieneOtraAsignacionEnMismaFecha = await _db.EventoProveedores
            .Include(ep => ep.EventoContratado)
            .AnyAsync(ep =>
                ep.Id != asignacionIdExcluida &&
                ep.ProveedorId == proveedorId &&
                ep.EventoContratado.FechaEvento.Date == fecha.Date &&
                ep.EventoContratado.Estatus != "cancelado",
                cancellationToken);

        if (!tieneOtraAsignacionEnMismaFecha)
        {
            var disp = await _db.ProveedorDisponibilidades
                .FirstOrDefaultAsync(pd => pd.ProveedorId == proveedorId && pd.Fecha.Date == fecha.Date, cancellationToken);

            if (disp != null)
            {
                _db.ProveedorDisponibilidades.Remove(disp);
            }
        }
    }

    private static List<(string Clave, string Nombre, string Descripcion, string CategoriaProveedor)> ObtenerRubrosBasePorCategoria(string categoriaEvento)
    {
        string norm = NormalizarTexto(categoriaEvento);
        if (norm.Contains("corporat"))
        {
            return new()
            {
                ("BASE-CATERING", "Banquete & Catering Ejecutivo", "Servicio de alimentos y bebidas incluido en paquete base", "Catering"),
                ("BASE-AUDIOVISUAL", "Audio, Iluminación y Proyección Base", "Sonido ambiental y microfonía del paquete", "Audiovisual"),
                ("BASE-MOBILIARIO", "Montaje y Mobiliario de Salón", "Mesas, sillas y mantelería institucional", "Mobiliario")
            };
        }

        return new()
        {
            ("BASE-CATERING", "Banquete y Servicio de Alimentos Base", "Menú incluido en el paquete contratado", "Catering"),
            ("BASE-MUSICA", "Música, DJ e Iluminación Ambiental", "Ambientación musical base del evento", "Música"),
            ("BASE-MOBILIARIO", "Mobiliario, Mesas y Mantelería", "Montaje principal para los invitados", "Mobiliario")
        };
    }

    private static bool EsCategoriaLocacion(string? categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria)) return false;
        string norm = NormalizarTexto(categoria);
        return norm.Contains("locacion") || norm.Contains("salon") || norm.Contains("jardin") || norm.Contains("hacienda") || norm.Contains("lugar");
    }

    private static bool CategoriasCoinciden(string? catProveedor, string? catRequerida)
    {
        if (string.IsNullOrWhiteSpace(catRequerida)) return true;
        if (string.IsNullOrWhiteSpace(catProveedor)) return false;

        string p = NormalizarTexto(catProveedor);
        string r = NormalizarTexto(catRequerida);

        if (p == r || p.Contains(r) || r.Contains(p)) return true;

        // Sinónimos frecuentes entre catálogo de servicios y categorías de proveedor
        if ((r.Contains("musica") || r.Contains("dj") || r.Contains("entretenimiento")) &&
            (p.Contains("musica") || p.Contains("dj") || p.Contains("entretenimiento") || p.Contains("audiovisual")))
        {
            return true;
        }

        if ((r.Contains("catering") || r.Contains("reposteria") || r.Contains("bebidas") || r.Contains("snacks")) &&
            (p.Contains("catering") || p.Contains("reposteria") || p.Contains("bebidas") || p.Contains("snacks") || p.Contains("banquete")))
        {
            return true;
        }

        if ((r.Contains("mobiliario") || r.Contains("decoracion") || r.Contains("floristeria")) &&
            (p.Contains("mobiliario") || p.Contains("decoracion") || p.Contains("floristeria") || p.Contains("iluminacion")))
        {
            return true;
        }

        return false;
    }

    private static string NormalizarTexto(string texto)
    {
        var normalized = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static decimal EstimarCostoPorRango(string? precioPromedio, bool esLocacion)
    {
        return (precioPromedio?.Trim()) switch
        {
            "$" => esLocacion ? 6000m : 2000m,
            "$$" => esLocacion ? 10000m : 3500m,
            "$$$" => esLocacion ? 15000m : 5500m,
            "$$$$" => esLocacion ? 24000m : 8500m,
            _ => esLocacion ? 10000m : 3500m
        };
    }
}

