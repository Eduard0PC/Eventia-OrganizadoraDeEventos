using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Server.Core.DTOs;
using Server.Core.Interfaces;

namespace Server.API.Endpoints;

public static class PagoEndpoints
{
    public static IEndpointRouteBuilder MapPagoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/pagos", async (int? clienteId, int? contratoId, IPagoService pagoService, CancellationToken cancellationToken) =>
        {
            var result = await pagoService.GetPagosAsync(clienteId, contratoId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetPagos");

        endpoints.MapGet("/api/pagos/cuotas-pendientes", async (int? clienteId, int? contratoId, IPagoService pagoService, CancellationToken cancellationToken) =>
        {
            var result = await pagoService.GetCuotasPendientesAsync(clienteId, contratoId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetCuotasPendientes");

        endpoints.MapGet("/api/pagos/resumen", async (int? clienteId, IPagoService pagoService, CancellationToken cancellationToken) =>
        {
            var result = await pagoService.GetResumenAsync(clienteId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetResumenPagos");

        endpoints.MapGet("/api/pagos/estado-cuenta", async (int? clienteId, int? contratoId, IPagoService pagoService, CancellationToken cancellationToken) =>
        {
            var result = await pagoService.GetEstadoCuentaAsync(clienteId, contratoId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetEstadoCuentaEventos");

        endpoints.MapPost("/api/pagos", async (RegistrarPagoRequest request, IPagoService pagoService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await pagoService.RegistrarPagoAsync(request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("RegistrarPago");

        endpoints.MapPost("/api/contratos/{contratoId:int}/generar-plan-pagos", async (int contratoId, IPagoService pagoService, CancellationToken cancellationToken) =>
        {
            var (success, error) = await pagoService.GenerarPlanPagosDefaultAsync(contratoId, cancellationToken);
            if (!success)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(new { mensaje = "Plan de pagos generado exitosamente." });
        })
        .WithName("GenerarPlanPagos");

        return endpoints;
    }
}
