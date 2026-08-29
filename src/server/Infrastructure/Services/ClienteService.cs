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

public class ClienteService : IClienteService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ClienteService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<ClienteSummaryResponse>> GetClientesAsync(CancellationToken cancellationToken = default)
    {
        var clientes = await _db.Clientes
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Apellido)
            .ToListAsync(cancellationToken);

        // Fetch contracts & events to calculate stats
        var contratos = await _db.Contratos
            .Include(c => c.EventosContratados)
                .ThenInclude(e => e.CatalogoEvento)
            .ToListAsync(cancellationToken);

        var cotizaciones = await _db.Cotizaciones
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoEvento)
            .ToListAsync(cancellationToken);

        var result = new List<ClienteSummaryResponse>();

        foreach (var c in clientes)
        {
            var summary = new ClienteSummaryResponse
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Apellido = c.Apellido,
                Email = c.Email,
                Telefono = c.Telefono,
                Activo = c.Activo
            };

            // Calculate Ultimo Evento Contratado from EventosContratados or Cotizaciones
            var clientContratos = contratos.Where(co => co.ClienteId == c.Id).ToList();
            var clientCotizaciones = cotizaciones.Where(cot => cot.ClienteId == c.Id).ToList();

            var clientEvents = clientContratos
                .SelectMany(co => co.EventosContratados)
                .OrderByDescending(ev => ev.FechaEvento)
                .ToList();

            if (clientEvents.Any())
            {
                var latestEvent = clientEvents.First();
                summary.UltimoEventoContratado = latestEvent.CatalogoEvento?.Nombre ?? "Evento Contratado";
                summary.FechaUltimoEvento = latestEvent.FechaEvento.ToString("yyyy-MM-dd");
                summary.TotalEventos = clientEvents.Count;
            }
            else
            {
                // Fallback to cotizaciones if no contract/event found
                var cotizaciónConEvento = clientCotizaciones
                    .Where(cot => cot.Items.Any(i => i.CatalogoEventoId.HasValue))
                    .OrderByDescending(cot => cot.CreatedAt)
                    .FirstOrDefault();

                if (cotizaciónConEvento != null)
                {
                    var itemEvento = cotizaciónConEvento.Items.FirstOrDefault(i => i.CatalogoEventoId.HasValue);
                    summary.UltimoEventoContratado = itemEvento?.CatalogoEvento?.Nombre ?? "Cotización de Evento";
                    summary.FechaUltimoEvento = cotizaciónConEvento.FechaEvento?.ToString("yyyy-MM-dd") ?? cotizaciónConEvento.CreatedAt.ToString("yyyy-MM-dd");
                }
                else
                {
                    summary.UltimoEventoContratado = "Sin eventos contratados";
                }

                summary.TotalEventos = clientContratos.Count > 0 ? clientContratos.Count : clientCotizaciones.Count;
            }

            result.Add(summary);
        }

        return result;
    }

    public async Task<ClienteDetalleResponse?> GetClienteFichaAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cliente == null) return null;

        var respuesta = new ClienteDetalleResponse
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Apellido = cliente.Apellido,
            Email = cliente.Email,
            Telefono = cliente.Telefono,
            Activo = cliente.Activo
        };

        // 1. Historial de Pagos
        var contratos = await _db.Contratos
            .Where(c => c.ClienteId == id)
            .Include(c => c.Pagos)
            .Include(c => c.EventosContratados)
                .ThenInclude(e => e.CatalogoEvento)
            .ToListAsync(cancellationToken);

        var pagosList = contratos
            .SelectMany(c => c.Pagos.Select(p => new PagoClienteDto
            {
                Id = p.Id,
                FolioContrato = c.Folio,
                Monto = p.Monto,
                MetodoPago = p.MetodoPago,
                TipoTransaccion = p.TipoTransaccion,
                FechaPago = p.FechaPago.ToString("yyyy-MM-dd"),
                Estatus = p.Estatus,
                Referencia = p.Referencia
            }))
            .OrderByDescending(p => p.FechaPago)
            .ToList();

        respuesta.HistorialPagos = pagosList;
        respuesta.TotalPagado = pagosList.Sum(p => p.Monto);

        // 2. Servicios / Cotizaciones Contratadas (Agrupados por Cotización Principal)
        var cotizaciones = await _db.Cotizaciones
            .Where(c => c.ClienteId == id)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoEvento)
            .Include(c => c.Items)
                .ThenInclude(i => i.CatalogoServicio)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var serviciosList = cotizaciones.Select(cot =>
        {
            var primerItemEvento = cot.Items.FirstOrDefault(i => i.Tipo == "evento");
            string nombrePrincipal = primerItemEvento?.CatalogoEvento?.Nombre
                ?? cot.Items.FirstOrDefault(i => i.CatalogoServicio != null)?.CatalogoServicio?.Nombre
                ?? cot.Items.FirstOrDefault()?.Notas
                ?? "Cotización de Evento / Servicios";

            decimal totalCot = cot.TotalFinal ?? cot.Total;

            return new ServicioContratadoDto
            {
                Id = cot.Id,
                Nombre = nombrePrincipal,
                Tipo = primerItemEvento != null ? "Paquete de Evento" : "Servicios",
                PrecioUnitario = totalCot,
                Cantidad = cot.Items.Count,
                Subtotal = totalCot,
                FechaCotizacion = cot.CreatedAt.ToString("yyyy-MM-dd"),
                FolioCotizacion = cot.Folio
            };
        }).ToList();

        respuesta.ServiciosContratados = serviciosList;

        // 3. Eventos Activos
        var eventosActivosList = contratos
            .SelectMany(c => c.EventosContratados.Select(e => new EventoActivoDto
            {
                Id = e.Id,
                NombreEvento = e.CatalogoEvento?.Nombre ?? "Evento",
                FechaEvento = e.FechaEvento.ToString("yyyy-MM-dd"),
                Lugar = e.Lugar,
                Aforo = e.Aforo,
                Estatus = e.Estatus,
                FolioContrato = c.Folio
            }))
            .OrderBy(e => e.FechaEvento)
            .ToList();

        // Also add upcoming cotizaciones if no active event is recorded in contratos
        if (!eventosActivosList.Any())
        {
            foreach (var cot in cotizaciones.Where(c => c.Estatus.ToLower() != "rechazada"))
            {
                var itemEvento = cot.Items.FirstOrDefault(i => i.Tipo.ToLower() == "evento");
                if (itemEvento != null)
                {
                    eventosActivosList.Add(new EventoActivoDto
                    {
                        Id = cot.Id,
                        NombreEvento = itemEvento.CatalogoEvento?.Nombre ?? "Evento",
                        FechaEvento = cot.FechaEvento?.ToString("yyyy-MM-dd") ?? cot.CreatedAt.ToString("yyyy-MM-dd"),
                        Lugar = null,
                        Aforo = cot.Invitados,
                        Estatus = cot.Estatus,
                        FolioContrato = cot.Folio
                    });
                }
            }
        }

        respuesta.EventosActivos = eventosActivosList;
        respuesta.TotalEventos = respuesta.EventosActivos.Count;

        var ultimoEvento = respuesta.EventosActivos.OrderByDescending(e => e.FechaEvento).FirstOrDefault();
        if (ultimoEvento != null)
        {
            respuesta.UltimoEventoContratado = ultimoEvento.NombreEvento;
        }

        return respuesta;
    }

    public async Task<(ClienteSummaryResponse? Result, string? Error)> CrearClienteAsync(CrearClienteRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
        {
            return (null, "El nombre y apellido son obligatorios.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return (null, "El email es obligatorio.");
        }

        var emailLower = request.Email.Trim().ToLowerInvariant();

        var existeCliente = await _db.Clientes.AnyAsync(c => c.Email.ToLower() == emailLower, cancellationToken);
        var existeUsuario = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == emailLower, cancellationToken);
        if (existeCliente || existeUsuario)
        {
            return (null, "Ya existe un usuario o cliente registrado con ese correo electrónico.");
        }

        var nuevoCliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Email = request.Email.Trim(),
            Telefono = request.Telefono?.Trim(),
            Activo = true
        };

        _db.Clientes.Add(nuevoCliente);
        await _db.SaveChangesAsync(cancellationToken);

        // Crear la cuenta de usuario asociada en la tabla 'usuarios' de Supabase
        var password = !string.IsNullOrWhiteSpace(request.Password) ? request.Password : "Cliente123!";
        var nuevoUsuario = new Usuario
        {
            Email = nuevoCliente.Email,
            PasswordHash = _passwordHasher.HashPassword(password),
            Rol = "cliente",
            ClienteId = nuevoCliente.Id,
            Activo = true
        };

        _db.Usuarios.Add(nuevoUsuario);
        await _db.SaveChangesAsync(cancellationToken);

        var summary = new ClienteSummaryResponse
        {
            Id = nuevoCliente.Id,
            Nombre = nuevoCliente.Nombre,
            Apellido = nuevoCliente.Apellido,
            Email = nuevoCliente.Email,
            Telefono = nuevoCliente.Telefono,
            Activo = nuevoCliente.Activo,
            UltimoEventoContratado = "Sin eventos contratados",
            TotalEventos = 0
        };

        return (summary, null);
    }
}
