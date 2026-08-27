using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Server.Core.DTOs;
using Server.Core.Interfaces;

namespace Server.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            var response = await authService.LoginAsync(request, cancellationToken);
            if (response is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        })
        .WithName("Login");

        return endpoints;
    }
}
