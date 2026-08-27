using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

        return endpoints;
    }
}
