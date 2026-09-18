using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Server.Core.DTOs;
using Server.Core.Interfaces;

namespace Server.API.Endpoints;

public static class ProveedorEndpoints
{
    public static IEndpointRouteBuilder MapProveedorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Proveedores CRUD
        endpoints.MapGet("/api/proveedores", async (int? categoriaId, bool? incluirInactivos, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var result = await proveedorService.GetProveedoresAsync(categoriaId, incluirInactivos ?? false, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetProveedores");

        endpoints.MapGet("/api/proveedores/categorias", async (bool? incluirInactivos, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var result = await proveedorService.GetCategoriasAsync(incluirInactivos ?? false, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetCategoriasProveedores");

        endpoints.MapGet("/api/proveedores/{id:int}", async (int id, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var result = await proveedorService.GetProveedorByIdAsync(id, cancellationToken);
            if (result is null) return Results.NotFound(new { error = "Proveedor no encontrado" });
            return Results.Ok(result);
        })
        .WithName("GetProveedorById");

        endpoints.MapPost("/api/proveedores", async (CrearProveedorRequest request, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await proveedorService.CrearProveedorAsync(request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/proveedores/{result!.Id}", result);
        })
        .WithName("CrearProveedor");

        endpoints.MapPut("/api/proveedores/{id:int}", async (int id, ActualizarProveedorRequest request, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await proveedorService.ActualizarProveedorAsync(id, request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("ActualizarProveedor");

        endpoints.MapDelete("/api/proveedores/{id:int}", async (int id, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var success = await proveedorService.DeleteProveedorAsync(id, cancellationToken);
            if (!success) return Results.NotFound(new { error = "Proveedor no encontrado" });
            return Results.NoContent();
        })
        .WithName("DeleteProveedor");

        // Servicios de Proveedor
        endpoints.MapGet("/api/proveedores/{id:int}/servicios", async (int id, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var result = await proveedorService.GetServiciosByProveedorIdAsync(id, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetServiciosByProveedorId");

        endpoints.MapPost("/api/proveedores/{id:int}/servicios", async (int id, CrearProveedorServicioRequest request, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await proveedorService.CrearServicioAsync(id, request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/proveedores/{id}/servicios/{result!.Id}", result);
        })
        .WithName("CrearProveedorServicio");

        endpoints.MapPut("/api/proveedores/servicios/{servicioId:int}", async (int servicioId, ActualizarProveedorServicioRequest request, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await proveedorService.ActualizarServicioAsync(servicioId, request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("ActualizarProveedorServicio");

        endpoints.MapDelete("/api/proveedores/servicios/{servicioId:int}", async (int servicioId, IProveedorService proveedorService, CancellationToken cancellationToken) =>
        {
            var success = await proveedorService.DeleteServicioAsync(servicioId, cancellationToken);
            if (!success) return Results.NotFound(new { error = "Servicio de proveedor no encontrado" });
            return Results.NoContent();
        })
        .WithName("DeleteProveedorServicio");

        return endpoints;
    }
}
