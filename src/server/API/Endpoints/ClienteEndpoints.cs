using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Server.Core.DTOs;
using Server.Core.Interfaces;

namespace Server.API.Endpoints;

public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClienteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/clientes", async (IClienteService clienteService, CancellationToken cancellationToken) =>
        {
            var result = await clienteService.GetClientesAsync(cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetClientes");

        endpoints.MapGet("/api/clientes/{id:int}/ficha", async (int id, IClienteService clienteService, CancellationToken cancellationToken) =>
        {
            var result = await clienteService.GetClienteFichaAsync(id, cancellationToken);
            if (result is null) return Results.NotFound(new { error = "Cliente no encontrado." });
            return Results.Ok(result);
        })
        .WithName("GetClienteFicha");

        endpoints.MapPost("/api/clientes", async (CrearClienteRequest request, IClienteService clienteService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await clienteService.CrearClienteAsync(request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/clientes/{result!.Id}", result);
        })
        .WithName("CrearCliente");

        return endpoints;
    }
}
