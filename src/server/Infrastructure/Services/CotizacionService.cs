using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Core.DTOs;
using Server.Core.Entities;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Infrastructure.Services;

public class CotizacionService : ICotizacionService
{
    /// <summary>Nombre del servicio interno (no visible al cliente) para cobrar invitados extra.</summary>
    private const string ServicioInvitadoAdicional = "Invitado adicional";
    /// <summary>Nombre del servicio interno (no visible al cliente) para cobrar horas extra.</summary>
    private const string ServicioHoraAdicional = "Hora adicional";

    private readonly AppDbContext _db;

    public CotizacionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CotizacionResponse>> GetCotizacionesAsync(int? clienteId, CancellationToken cancellationToken = default)
    {
        await ExpirarBorradoresAntiguosAsync(cancellationToken);

        var query = _db.Cotizaciones
            .Include(c => c.Cliente)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoEvento)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoServicio!)
                    .ThenInclude(cs => cs.CategoriaProveedor)
            .AsQueryable();

        if (clienteId.HasValue && clienteId.Value > 0)
        {
            query = query.Where(c => c.ClienteId == clienteId.Value);
        }

        var cotizaciones = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return cotizaciones.Select(MapToResponse).ToList();
    }

    public async Task<CotizacionResponse?> GetCotizacionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await ExpirarBorradoresAntiguosAsync(cancellationToken);

        var c = await CargarCotizacionCompletaAsync(id, cancellationToken);
        if (c is null) return null;

        return MapToResponse(c);
    }

    public async Task<(CotizacionResponse? Result, string? Error)> CrearCotizacionAsync(CrearCotizacionRequest request, CancellationToken cancellationToken = default)
    {
        await ExpirarBorradoresAntiguosAsync(cancellationToken);

        var evento = await _db.CatalogoEventos.FindAsync(new object[] { request.CatalogoEventoId }, cancellationToken);
        if (evento is null)
        {
            return (null, "El evento especificado no existe en el catálogo.");
        }

        int targetClienteId = request.ClienteId ?? 0;
        if (targetClienteId <= 0)
        {
            var primerCliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Activo, cancellationToken);
            if (primerCliente != null)
            {
                targetClienteId = primerCliente.Id;
            }
            else
            {
                return (null, "No hay clientes activos registrados.");
            }
        }

        var (items, totalCotizacion, errorItems) = await ConstruirItemsAsync(evento, request.Invitados, request.HorasAdicionales, request.ServiciosExtraIds, cancellationToken);
        if (errorItems is not null)
        {
            return (null, errorItems);
        }

        string estatusFinal = !string.IsNullOrWhiteSpace(request.Estatus) ? request.Estatus.ToLower() : "enviada";
        DateTime? fechaEventoParsed = ParsearFecha(request.FechaEvento);

        // Transacción: la cotización y sus renglones se guardan juntos o no se guarda nada
        // (evita cotizaciones con total pero sin items si falla la inserción de renglones).
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        Cotizacion? cotizacionDestino = null;

        if (estatusFinal == "borrador")
        {
            cotizacionDestino = await _db.Cotizaciones
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.ClienteId == targetClienteId && c.Estatus == "borrador" && c.Items.Any(i => i.CatalogoEventoId == request.CatalogoEventoId), cancellationToken);

            if (cotizacionDestino != null)
            {
                cotizacionDestino.Total = totalCotizacion;
                cotizacionDestino.FechaEvento = fechaEventoParsed;
                cotizacionDestino.Invitados = request.Invitados;
                cotizacionDestino.Notas = request.Notas;
                cotizacionDestino.UpdatedAt = DateTime.UtcNow;

                _db.CotizacionItems.RemoveRange(cotizacionDestino.Items);
                cotizacionDestino.Items.Clear();
            }
        }

        if (cotizacionDestino is null)
        {
            cotizacionDestino = new Cotizacion
            {
                ClienteId = targetClienteId,
                Folio = GenerarFolio(),
                Total = totalCotizacion,
                Descuento = 0m,
                Estatus = estatusFinal,
                FechaVigencia = DateTime.UtcNow.AddDays(30),
                FechaEvento = fechaEventoParsed,
                Invitados = request.Invitados,
                Notas = request.Notas,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Cotizaciones.Add(cotizacionDestino);
        }

        // Primer guardado: obtiene el Id de la cotización y aplica el borrado de renglones previos
        // antes de insertar los nuevos (evita choque con el índice único por servicio).
        await _db.SaveChangesAsync(cancellationToken);

        AgregarItems(cotizacionDestino.Id, items);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var cGuardada = await CargarCotizacionCompletaAsync(cotizacionDestino.Id, cancellationToken);
        return (MapToResponse(cGuardada!), null);
    }

    private async Task ExpirarBorradoresAntiguosAsync(CancellationToken cancellationToken)
    {
        try
        {
            var fechaLimite = DateTime.UtcNow.AddDays(-30);
            var borradoresVencidos = await _db.Cotizaciones
                .Where(c => c.Estatus == "borrador" && c.UpdatedAt < fechaLimite)
                .ToListAsync(cancellationToken);

            if (borradoresVencidos.Count > 0)
            {
                foreach (var cot in borradoresVencidos)
                {
                    cot.Estatus = "vencida";
                    cot.UpdatedAt = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log expiration error silently without failing the main request
            System.Diagnostics.Debug.WriteLine($"Error en ExpirarBorradoresAntiguos: {ex.Message}");
        }
    }

    public async Task<(CotizacionResponse? Result, string? Error)> ActualizarCotizacionAsync(int id, CrearCotizacionRequest request, CancellationToken cancellationToken = default)
    {
        await ExpirarBorradoresAntiguosAsync(cancellationToken);

        var cotizacion = await _db.Cotizaciones
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cotizacion is null)
        {
            return (null, "La cotización especificada no existe.");
        }

        if (cotizacion.Estatus != "borrador" && cotizacion.Estatus != "enviada" && cotizacion.Estatus != "modificada")
        {
            return (null, "Solo se pueden editar cotizaciones que esten en estatus borrador, enviada o modificada.");
        }

        var evento = await _db.CatalogoEventos.FindAsync(new object[] { request.CatalogoEventoId }, cancellationToken);
        if (evento is null)
        {
            return (null, "El evento especificado no existe en el catálogo.");
        }

        var (items, totalCotizacion, errorItems) = await ConstruirItemsAsync(evento, request.Invitados, request.HorasAdicionales, request.ServiciosExtraIds, cancellationToken);
        if (errorItems is not null)
        {
            return (null, errorItems);
        }

        string estatusFinal = !string.IsNullOrWhiteSpace(request.Estatus) ? request.Estatus.ToLower() : cotizacion.Estatus;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        cotizacion.Total = totalCotizacion;
        cotizacion.Estatus = estatusFinal;
        cotizacion.FechaEvento = ParsearFecha(request.FechaEvento);
        cotizacion.Invitados = request.Invitados;
        cotizacion.Notas = request.Notas;
        cotizacion.UpdatedAt = DateTime.UtcNow;

        _db.CotizacionItems.RemoveRange(cotizacion.Items);
        cotizacion.Items.Clear();
        await _db.SaveChangesAsync(cancellationToken);

        AgregarItems(cotizacion.Id, items);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var cActualizada = await CargarCotizacionCompletaAsync(id, cancellationToken);
        return (MapToResponse(cActualizada!), null);
    }

    public async Task<(AprobarCotizacionResponse? Result, string? Error)> AprobarCotizacionAsync(int id, CancellationToken cancellationToken = default)
    {
        var cotizacion = await _db.Cotizaciones
            .Include(c => c.Cliente)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cotizacion is null)
        {
            return (null, "La cotizacion especificada no existe.");
        }

        var contratoExistente = await _db.Contratos
            .Include(c => c.EventosContratados)
            .FirstOrDefaultAsync(c => c.CotizacionId == cotizacion.Id, cancellationToken);

        if (contratoExistente != null)
        {
            var ev = contratoExistente.EventosContratados.FirstOrDefault();
            return (new AprobarCotizacionResponse(
                cotizacion.Id,
                cotizacion.Folio,
                cotizacion.Estatus,
                contratoExistente.Id,
                contratoExistente.Folio,
                ev?.Id ?? 0,
                "La cotizacion ya cuenta con un contrato formalizado previamente."
            ), null);
        }

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            cotizacion.Estatus = "aceptada";
            cotizacion.UpdatedAt = DateTime.UtcNow;

            int anioActual = DateTime.UtcNow.Year;
            int conteoContratos = await _db.Contratos.CountAsync(c => c.Folio.StartsWith($"CTR-{anioActual}-"), cancellationToken);
            string folioContrato = $"CTR-{anioActual}-{(conteoContratos + 1):D4}";

            DateTime fechaEv = cotizacion.FechaEvento.HasValue
                ? DateTime.SpecifyKind(cotizacion.FechaEvento.Value, DateTimeKind.Utc)
                : DateTime.UtcNow.AddDays(15);

            var contrato = new Contrato
            {
                ClienteId = cotizacion.ClienteId,
                CotizacionId = cotizacion.Id,
                Folio = folioContrato,
                FechaFirma = DateTime.UtcNow.Date,
                FechaInicio = fechaEv.Date,
                FechaFin = fechaEv.Date,
                TotalContrato = cotizacion.TotalFinal ?? cotizacion.Total,
                Estatus = "activo",
                Condiciones = cotizacion.Notas,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Contratos.Add(contrato);
            await _db.SaveChangesAsync(cancellationToken);

            int catalogoEventoId = cotizacion.Items.FirstOrDefault(i => i.CatalogoEventoId.HasValue)?.CatalogoEventoId
                ?? await _db.CatalogoEventos.Where(e => e.Activo).Select(e => e.Id).FirstOrDefaultAsync(cancellationToken);

            var eventoContratado = new EventoContratado
            {
                ContratoId = contrato.Id,
                CatalogoEventoId = catalogoEventoId,
                FechaEvento = fechaEv.Date,
                Lugar = "Por confirmar con cliente",
                Aforo = (short?)cotizacion.Invitados,
                Estatus = "programado",
                Notas = cotizacion.Notas,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.EventosContratados.Add(eventoContratado);
            await _db.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return (new AprobarCotizacionResponse(
                cotizacion.Id,
                cotizacion.Folio,
                cotizacion.Estatus,
                contrato.Id,
                contrato.Folio,
                eventoContratado.Id,
                "Cotizacion aprobada y contrato formalizado exitosamente."
            ), null);
        }
        catch (Exception ex)
        {
            return (null, $"Error al aprobar la cotización y generar contrato: {ex.Message}");
        }
    }

    public async Task<(CotizacionResponse? Result, string? Error)> RechazarCotizacionAsync(int id, string? motivo, CancellationToken cancellationToken = default)
    {
        var cotizacion = await CargarCotizacionCompletaAsync(id, cancellationToken);
        if (cotizacion is null)
        {
            return (null, "La cotizacion especificada no existe.");
        }

        cotizacion.Estatus = "rechazada";
        if (!string.IsNullOrWhiteSpace(motivo))
        {
            string textoMotivo = $"[Motivo de rechazo]: {motivo.Trim()}";
            cotizacion.Notas = string.IsNullOrWhiteSpace(cotizacion.Notas)
                ? textoMotivo
                : $"{cotizacion.Notas}\n{textoMotivo}";
        }
        cotizacion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (MapToResponse(cotizacion), null);
    }

    public async Task<bool> DeleteCotizacionAsync(int id, CancellationToken cancellationToken = default)
    {
        var cotizacion = await _db.Cotizaciones.FindAsync(new object[] { id }, cancellationToken);
        if (cotizacion is null) return false;

        _db.Cotizaciones.Remove(cotizacion);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ==========================================
    // HELPERS
    // ==========================================

    /// <summary>
    /// Construye los renglones de la cotización (paquete + invitados extra + horas extra)
    /// y calcula el total. Los cargos extra se ligan a los servicios internos de
    /// catalogo_servicios para cumplir con chk_item_tipo_ref y tomar su precio de la BD.
    /// </summary>
    private async Task<(List<CotizacionItem> Items, decimal Total, string? Error)> ConstruirItemsAsync(
        CatalogoEvento evento, int invitados, int horasAdicionales, List<int>? serviciosExtraIds, CancellationToken cancellationToken)
    {
        if (invitados < 0) return (new(), 0m, "El número de invitados no puede ser negativo.");
        if (horasAdicionales < 0) return (new(), 0m, "Las horas adicionales no pueden ser negativas.");

        int capacidadBase = evento.CapacidadPersonas > 0 ? evento.CapacidadPersonas : 50;
        int extraInvitados = Math.Max(0, invitados - capacidadBase);

        if (extraInvitados > short.MaxValue || horasAdicionales > short.MaxValue)
        {
            return (new(), 0m, "La cantidad de invitados u horas adicionales excede el máximo permitido.");
        }

        var utcNow = DateTime.UtcNow;
        var items = new List<CotizacionItem>
        {
            new()
            {
                Tipo = "evento",
                Cantidad = 1,
                PrecioUnitario = evento.PrecioBase,
                DescuentoItem = 0m,
                Notas = $"Paquete {evento.Nombre} - Base: {capacidadBase} personas",
                CatalogoEventoId = evento.Id,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            }
        };

        if (extraInvitados > 0 || horasAdicionales > 0)
        {
            var internos = await _db.CatalogoServicios
                .Where(s => s.Activo && !s.VisibleCliente
                            && (s.Nombre == ServicioInvitadoAdicional || s.Nombre == ServicioHoraAdicional))
                .ToListAsync(cancellationToken);

            if (extraInvitados > 0)
            {
                var servicio = internos.FirstOrDefault(s => s.Nombre == ServicioInvitadoAdicional);
                if (servicio is null)
                {
                    return (new(), 0m, $"No está configurado el servicio interno \"{ServicioInvitadoAdicional}\" en el catálogo de servicios.");
                }

                items.Add(new CotizacionItem
                {
                    Tipo = "servicio",
                    Cantidad = (short)extraInvitados,
                    PrecioUnitario = servicio.PrecioBase,
                    DescuentoItem = 0m,
                    Notas = $"Invitados adicionales ({extraInvitados} personas)",
                    CatalogoServicioId = servicio.Id,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                });
            }

            if (horasAdicionales > 0)
            {
                var servicio = internos.FirstOrDefault(s => s.Nombre == ServicioHoraAdicional);
                if (servicio is null)
                {
                    return (new(), 0m, $"No está configurado el servicio interno \"{ServicioHoraAdicional}\" en el catálogo de servicios.");
                }

                items.Add(new CotizacionItem
                {
                    Tipo = "servicio",
                    Cantidad = (short)horasAdicionales,
                    PrecioUnitario = servicio.PrecioBase,
                    DescuentoItem = 0m,
                    Notas = $"Horas adicionales ({horasAdicionales} hrs)",
                    CatalogoServicioId = servicio.Id,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                });
            }
        }

        if (serviciosExtraIds != null && serviciosExtraIds.Count > 0)
        {
            var distinctIds = serviciosExtraIds.Distinct().ToList();
            var extrasDb = await _db.CatalogoServicios
                .Include(s => s.CategoriaProveedor)
                .Where(s => s.Activo && s.VisibleCliente && distinctIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var eventoCat = evento.Categoria?.Trim().ToLower();

            foreach (var sId in distinctIds)
            {
                var servicio = extrasDb.FirstOrDefault(s => s.Id == sId);
                if (servicio == null)
                {
                    return (new(), 0m, $"El servicio extra solicitado con ID {sId} no existe o no está disponible.");
                }

                if (!string.IsNullOrWhiteSpace(servicio.Categoria) &&
                    !string.IsNullOrWhiteSpace(eventoCat) &&
                    servicio.Categoria.Trim().ToLower() != eventoCat)
                {
                    return (new(), 0m, $"El servicio extra \"{servicio.Nombre}\" no corresponde a la categoría \"{evento.Categoria}\".");
                }

                items.Add(new CotizacionItem
                {
                    Tipo = "servicio",
                    Cantidad = 1,
                    PrecioUnitario = servicio.PrecioBase,
                    DescuentoItem = 0m,
                    Notas = servicio.Nombre,
                    CatalogoServicioId = servicio.Id,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                });
            }
        }

        decimal total = items.Sum(i => i.Cantidad * i.PrecioUnitario - i.DescuentoItem);
        return (items, total, null);
    }

    private void AgregarItems(int cotizacionId, List<CotizacionItem> items)
    {
        foreach (var item in items)
        {
            item.CotizacionId = cotizacionId;
            _db.CotizacionItems.Add(item);
        }
    }

    private Task<Cotizacion?> CargarCotizacionCompletaAsync(int id, CancellationToken cancellationToken)
    {
        return _db.Cotizaciones
            .Include(c => c.Cliente)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoEvento)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoServicio!)
                    .ThenInclude(cs => cs.CategoriaProveedor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static DateTime? ParsearFecha(string? fecha)
    {
        return DateTime.TryParse(fecha, out var parsedDate)
            ? DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc)
            : null;
    }

    private static string GenerarFolio()
    {
        int randomNum = Random.Shared.Next(1000, 9999);
        return $"COT-{DateTime.UtcNow.Year}-{randomNum}";
    }

    private CotizacionResponse MapToResponse(Cotizacion c)
    {
        var primerItemEvento = c.Items.FirstOrDefault(i => i.Tipo == "evento");
        var nombreEvento = primerItemEvento?.CatalogoEvento?.Nombre ?? "Evento personalizado";

        var itemsDto = c.Items
            .OrderBy(i => i.Tipo == "evento" ? 0 : 1)
            .ThenBy(i => i.Id)
            .Select(i =>
        {
            string desc = !string.IsNullOrWhiteSpace(i.Notas)
                ? i.Notas
                : (i.CatalogoEvento != null ? $"Paquete {i.CatalogoEvento.Nombre}" : (i.CatalogoServicio != null ? i.CatalogoServicio.Nombre : "Servicio contratado"));

            decimal subtotal = i.Subtotal ?? (i.Cantidad * i.PrecioUnitario - i.DescuentoItem);

            bool esServicioExtra = i.Tipo == "servicio"
                && i.CatalogoServicio != null
                && i.CatalogoServicio.VisibleCliente;

            string? catProv = i.CatalogoServicio?.CategoriaProveedor?.Nombre;

            return new CotizacionItemDto(
                i.Id,
                i.Tipo ?? "evento",
                desc,
                i.PrecioUnitario,
                i.Cantidad,
                i.DescuentoItem,
                subtotal,
                i.Notas,
                i.CatalogoServicioId,
                esServicioExtra,
                catProv
            );
        }).ToList();

        List<ServicioDefectoDto>? serviciosDefecto = null;
        var categoria = primerItemEvento?.CatalogoEvento?.Categoria;
        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var catNorm = categoria.Trim().ToLower();
            serviciosDefecto = _db.CatalogoEventoServicios
                .Include(ces => ces.CatalogoServicio)
                    .ThenInclude(cs => cs!.CategoriaProveedor)
                .Where(ces => ces.Categoria.ToLower() == catNorm)
                .OrderBy(ces => ces.OrdenPrioridad)
                .Select(ces => new ServicioDefectoDto(
                    ces.Id,
                    ces.CatalogoServicioId,
                    ces.CatalogoServicio != null ? ces.CatalogoServicio.Nombre : "Servicio",
                    ces.CatalogoServicio != null ? ces.CatalogoServicio.Descripcion : null,
                    ces.Categoria,
                    ces.CatalogoServicio != null && ces.CatalogoServicio.CategoriaProveedor != null ? ces.CatalogoServicio.CategoriaProveedor.Nombre : null,
                    ces.EsObligatorio,
                    ces.IncluidoEnPrecio,
                    ces.OrdenPrioridad,
                    ces.Notas
                ))
                .ToList();
        }

        return new CotizacionResponse(
            c.Id,
            c.Folio,
            nombreEvento,
            primerItemEvento?.CatalogoEventoId,
            c.FechaEvento?.ToString("yyyy-MM-dd"),
            c.Invitados,
            c.Total,
            c.Descuento,
            c.TotalFinal ?? (c.Total - c.Descuento),
            c.Estatus,
            c.FechaVigencia.ToString("yyyy-MM-dd"),
            c.CreatedAt.ToString("yyyy-MM-dd"),
            c.Notas ?? "",
            itemsDto,
            primerItemEvento?.CatalogoEvento?.Categoria,
            serviciosDefecto
        );
    }
}
