using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Server.Core.DTOs;
using Server.Core.Interfaces;

namespace Server.API.Endpoints;

public static class PlanificadorEndpoints
{
    public static IEndpointRouteBuilder MapPlanificadorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/planificador").WithTags("Planificador");

        group.MapGet("/eventos", async (IPlanificadorService planificadorService, CancellationToken cancellationToken) =>
        {
            var result = await planificadorService.GetPlanificacionesAsync(cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetPlanificacionesEventos");

        group.MapGet("/eventos/{eventoId:int}", async (int eventoId, IPlanificadorService planificadorService, CancellationToken cancellationToken) =>
        {
            var result = await planificadorService.GetPlanificacionByEventoIdAsync(eventoId, cancellationToken);
            if (result is null) return Results.NotFound(new { error = "Evento contratado no encontrado." });
            return Results.Ok(result);
        })
        .WithName("GetPlanificacionEventoById");

        group.MapGet("/eventos/{eventoId:int}/proveedores-disponibles", async (
            int eventoId,
            string? categoria,
            bool? esLocacion,
            IPlanificadorService planificadorService,
            CancellationToken cancellationToken) =>
        {
            var result = await planificadorService.GetProveedoresDisponiblesParaEventoAsync(
                eventoId,
                categoria,
                esLocacion ?? false,
                cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetProveedoresDisponiblesParaEvento");

        group.MapPost("/eventos/{eventoId:int}/asignaciones", async (
            int eventoId,
            AsignarProveedorEventoRequest request,
            IPlanificadorService planificadorService,
            CancellationToken cancellationToken) =>
        {
            var (result, error) = await planificadorService.AsignarProveedorAsync(eventoId, request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("AsignarProveedorAEvento");

        group.MapDelete("/eventos/{eventoId:int}/asignaciones/{asignacionId:int}", async (
            int eventoId,
            int asignacionId,
            IPlanificadorService planificadorService,
            CancellationToken cancellationToken) =>
        {
            var (result, error) = await planificadorService.RemoverAsignacionProveedorAsync(eventoId, asignacionId, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("RemoverAsignacionProveedorEvento");

        group.MapPost("/eventos/{eventoId:int}/tareas", async (
            int eventoId,
            CrearEventoTareaRequest request,
            IPlanificadorService planificadorService,
            CancellationToken cancellationToken) =>
        {
            var (result, error) = await planificadorService.CrearTareaAsync(eventoId, request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/planificador/eventos/{eventoId}/tareas/{result!.Id}", result);
        })
        .WithName("CrearTareaChecklistEvento");

        group.MapPatch("/eventos/{eventoId:int}/tareas/{tareaId:int}/toggle", async (
            int eventoId,
            int tareaId,
            IPlanificadorService planificadorService,
            CancellationToken cancellationToken) =>
        {
            var (result, error) = await planificadorService.ToggleTareaAsync(eventoId, tareaId, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("ToggleTareaChecklistEvento");

        group.MapDelete("/eventos/{eventoId:int}/tareas/{tareaId:int}", async (
            int eventoId,
            int tareaId,
            IPlanificadorService planificadorService,
            CancellationToken cancellationToken) =>
        {
            var deleted = await planificadorService.EliminarTareaAsync(eventoId, tareaId, cancellationToken);
            if (!deleted) return Results.NotFound(new { error = "Tarea no encontrada." });
            return Results.NoContent();
        })
        .WithName("EliminarTareaChecklistEvento");

        return endpoints;
    }
}

