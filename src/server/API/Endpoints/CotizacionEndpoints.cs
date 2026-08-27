using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Server.Core.DTOs;
using Server.Core.Interfaces;

namespace Server.API.Endpoints;

public static class CotizacionEndpoints
{
    public static IEndpointRouteBuilder MapCotizacionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/cotizaciones", async (int? clienteId, ICotizacionService cotizacionService, CancellationToken cancellationToken) =>
        {
            var result = await cotizacionService.GetCotizacionesAsync(clienteId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetCotizaciones");

        endpoints.MapGet("/api/cotizaciones/{id:int}", async (int id, ICotizacionService cotizacionService, CancellationToken cancellationToken) =>
        {
            var result = await cotizacionService.GetCotizacionByIdAsync(id, cancellationToken);
            if (result is null) return Results.NotFound();
            return Results.Ok(result);
        })
        .WithName("GetCotizacionById");

        endpoints.MapPost("/api/cotizaciones", async (CrearCotizacionRequest request, ICotizacionService cotizacionService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await cotizacionService.CrearCotizacionAsync(request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/cotizaciones/{result!.Id}", result);
        })
        .WithName("CrearCotizacion");

        endpoints.MapDelete("/api/cotizaciones/{id:int}", async (int id, ICotizacionService cotizacionService, CancellationToken cancellationToken) =>
        {
            var success = await cotizacionService.DeleteCotizacionAsync(id, cancellationToken);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        })
        .WithName("DeleteCotizacion");

        return endpoints;
    }
}
