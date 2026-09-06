using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Server.Core.DTOs;
using Server.Core.Interfaces;

namespace Server.API.Endpoints;

public static class CatalogoEndpoints
{
    public static IEndpointRouteBuilder MapCatalogoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/catalogo-eventos", async (ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var eventos = await catalogoService.GetCatalogoEventosAsync(cancellationToken);
            return Results.Ok(eventos);
        })
        .WithName("GetCatalogoEventos");

        endpoints.MapGet("/api/catalogo-eventos/{id:int}", async (int id, ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var evento = await catalogoService.GetCatalogoEventoByIdAsync(id, cancellationToken);
            if (evento is null)
            {
                return Results.NotFound(new { error = "Evento de catálogo no encontrado." });
            }
            return Results.Ok(evento);
        })
        .WithName("GetCatalogoEventoById");

        endpoints.MapPost("/api/catalogo-eventos", async (CrearCatalogoEventoRequest request, ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await catalogoService.CrearCatalogoEventoAsync(request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/catalogo-eventos/{result!.Id}", result);
        })
        .WithName("CrearCatalogoEvento");

        endpoints.MapPut("/api/catalogo-eventos/{id:int}", async (int id, ActualizarCatalogoEventoRequest request, ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await catalogoService.ActualizarCatalogoEventoAsync(id, request, cancellationToken);
            if (error != null)
            {
                if (error == "Evento de catálogo no encontrado.")
                {
                    return Results.NotFound(new { error });
                }
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("ActualizarCatalogoEvento");

        endpoints.MapDelete("/api/catalogo-eventos/{id:int}", async (int id, ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var success = await catalogoService.DeleteCatalogoEventoAsync(id, cancellationToken);
            if (!success)
            {
                return Results.NotFound(new { error = "Evento de catálogo no encontrado." });
            }
            return Results.NoContent();
        })
        .WithName("DeleteCatalogoEvento");

        // ==========================================
        // ENDPOINTS DE SERVICIOS DE CATÁLOGO
        // ==========================================

        endpoints.MapGet("/api/catalogo-servicios", async (ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var servicios = await catalogoService.GetCatalogoServiciosAsync(cancellationToken);
            return Results.Ok(servicios);
        })
        .WithName("GetCatalogoServicios");

        endpoints.MapGet("/api/catalogo-servicios/{id:int}", async (int id, ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var servicio = await catalogoService.GetCatalogoServicioByIdAsync(id, cancellationToken);
            if (servicio is null)
            {
                return Results.NotFound(new { error = "Servicio de catálogo no encontrado." });
            }
            return Results.Ok(servicio);
        })
        .WithName("GetCatalogoServicioById");

        endpoints.MapPost("/api/catalogo-servicios", async (CrearCatalogoServicioRequest request, ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await catalogoService.CrearCatalogoServicioAsync(request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/catalogo-servicios/{result!.Id}", result);
        })
        .WithName("CrearCatalogoServicio");

        endpoints.MapPut("/api/catalogo-servicios/{id:int}", async (int id, ActualizarCatalogoServicioRequest request, ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await catalogoService.ActualizarCatalogoServicioAsync(id, request, cancellationToken);
            if (error != null)
            {
                if (error == "Servicio de catálogo no encontrado.")
                {
                    return Results.NotFound(new { error });
                }
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("ActualizarCatalogoServicio");

        endpoints.MapDelete("/api/catalogo-servicios/{id:int}", async (int id, ICatalogoService catalogoService, CancellationToken cancellationToken) =>
        {
            var success = await catalogoService.DeleteCatalogoServicioAsync(id, cancellationToken);
            if (!success)
            {
                return Results.NotFound(new { error = "Servicio de catálogo no encontrado." });
            }
            return Results.NoContent();
        })
        .WithName("DeleteCatalogoServicio");

        return endpoints;
    }
}


