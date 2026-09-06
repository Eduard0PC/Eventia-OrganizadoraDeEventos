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

        endpoints.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await authService.RegisterAsync(request, cancellationToken);
            if (error != null)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/usuarios/{result!.Id}", result);
        })
        .WithName("Register");

        // ==========================================
        // ENDPOINTS DE GESTIÓN DE USUARIOS
        // ==========================================

        endpoints.MapGet("/api/usuarios", async (IAuthService authService, CancellationToken cancellationToken) =>
        {
            var usuarios = await authService.GetUsuariosAsync(cancellationToken);
            return Results.Ok(usuarios);
        })
        .WithName("GetUsuarios");

        endpoints.MapGet("/api/usuarios/{id:int}", async (int id, IAuthService authService, CancellationToken cancellationToken) =>
        {
            var usuario = await authService.GetUsuarioByIdAsync(id, cancellationToken);
            if (usuario is null)
            {
                return Results.NotFound(new { error = "Usuario no encontrado." });
            }
            return Results.Ok(usuario);
        })
        .WithName("GetUsuarioById");

        endpoints.MapPut("/api/usuarios/{id:int}", async (int id, ActualizarUsuarioRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            var (result, error) = await authService.ActualizarUsuarioAsync(id, request, cancellationToken);
            if (error != null)
            {
                if (error == "Usuario no encontrado.")
                {
                    return Results.NotFound(new { error });
                }
                return Results.BadRequest(new { error });
            }

            return Results.Ok(result);
        })
        .WithName("ActualizarUsuario");

        endpoints.MapPatch("/api/usuarios/{id:int}/estado", async (int id, CambiarEstadoUsuarioRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            var success = await authService.CambiarEstadoUsuarioAsync(id, request.Activo, cancellationToken);
            if (!success)
            {
                return Results.NotFound(new { error = "Usuario no encontrado." });
            }
            return Results.NoContent();
        })
        .WithName("CambiarEstadoUsuario");

        return endpoints;
    }
}

