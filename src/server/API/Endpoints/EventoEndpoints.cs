using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Server.Core.DTOs;
using Server.Core.Interfaces;

namespace Server.API.Endpoints;

public static class EventoEndpoints
{
    public static IEndpointRouteBuilder MapEventoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/eventos-contratados", async (int? clienteId, IEventoService eventoService, CancellationToken cancellationToken) =>
        {
            var result = await eventoService.GetEventosContratadosAsync(clienteId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetEventosContratados");

        endpoints.MapGet("/api/eventos-contratados/{id:int}", async (int id, IEventoService eventoService, CancellationToken cancellationToken) =>
        {
            var result = await eventoService.GetEventoContratadoByIdAsync(id, cancellationToken);
            if (result is null) return Results.NotFound(new { error = "Evento contratado no encontrado." });
            return Results.Ok(result);
        })
        .WithName("GetEventoContratadoById");

        endpoints.MapPut("/api/eventos-contratados/{id:int}/estatus", async (int id, ActualizarEstatusEventoRequest request, IEventoService eventoService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await eventoService.ActualizarEstatusEventoAsync(id, request.Estatus, request.Notas, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("ActualizarEstatusEvento");

        endpoints.MapGet("/api/eventos-contratados/{id:int}/contrato", async (int id, IEventoService eventoService, CancellationToken cancellationToken) =>
        {
            var result = await eventoService.GetContratoDocumentoByEventoIdAsync(id, cancellationToken);
            if (result is null) return Results.NotFound(new { error = "Contrato asociado al evento no encontrado." });
            return Results.Ok(result);
        })
        .WithName("GetContratoDocumentoByEventoId");

        endpoints.MapGet("/api/contratos/{contratoId:int}", async (int contratoId, IEventoService eventoService, CancellationToken cancellationToken) =>
        {
            var result = await eventoService.GetContratoDocumentoByContratoIdAsync(contratoId, cancellationToken);
            if (result is null) return Results.NotFound(new { error = "Contrato no encontrado." });
            return Results.Ok(result);
        })
        .WithName("GetContratoDocumentoById");

        return endpoints;
    }
}
