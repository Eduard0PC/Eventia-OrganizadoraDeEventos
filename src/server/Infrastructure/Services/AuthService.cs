using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Core.DTOs;
using Server.Core.Entities;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .Include(u => u.Cliente)
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email &&
                u.Activo &&
                (u.Rol == "organizador" || u.Rol == "admin" || (u.ClienteId != null && u.Cliente != null && u.Cliente.Activo)),
                cancellationToken);

        if (usuario is null || !_passwordHasher.VerifyPassword(request.Password, usuario.PasswordHash))
        {
            return null;
        }

        usuario.UltimoAcceso = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            usuario.Id,
            usuario.Email,
            usuario.Rol,
            usuario.Cliente is null
                ? null
                : new ClienteResponse(
                    usuario.Cliente.Id,
                    usuario.Cliente.Nombre,
                    usuario.Cliente.Apellido,
                    usuario.Cliente.Email,
                    usuario.Cliente.Telefono));
    }

    public async Task<(UsuarioResponse? Result, string? Error)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existe = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email, cancellationToken);
        if (existe)
        {
            return (null, "El correo electrónico ya se encuentra registrado.");
        }

        var rol = string.IsNullOrWhiteSpace(request.Rol) ? "cliente" : request.Rol.Trim().ToLowerInvariant();

        Cliente? cliente = null;
        if (rol == "cliente")
        {
            cliente = new Cliente
            {
                Nombre = request.Nombre.Trim(),
                Apellido = request.Apellido.Trim(),
                Email = email,
                Telefono = request.Telefono?.Trim(),
                Activo = true
            };
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var usuario = new Usuario
        {
            Email = email,
            PasswordHash = passwordHash,
            Rol = rol,
            ClienteId = cliente?.Id,
            Activo = true,
            UltimoAcceso = null
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(cancellationToken);

        var response = MapToUsuarioResponse(usuario, cliente);
        return (response, null);
    }

    public async Task<IEnumerable<UsuarioResponse>> GetUsuariosAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await _db.Usuarios
            .Include(u => u.Cliente)
            .OrderByDescending(u => u.Id)
            .ToListAsync(cancellationToken);

        return usuarios.Select(u => MapToUsuarioResponse(u, u.Cliente));
    }

    public async Task<UsuarioResponse?> GetUsuarioByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Cliente)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario is null) return null;

        return MapToUsuarioResponse(usuario, usuario.Cliente);
    }

    public async Task<(UsuarioResponse? Result, string? Error)> ActualizarUsuarioAsync(int id, ActualizarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Cliente)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario is null)
        {
            return (null, "Usuario no encontrado.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.Trim().ToLowerInvariant() != usuario.Email.ToLower())
        {
            var newEmail = request.Email.Trim().ToLowerInvariant();
            var existeEmail = await _db.Usuarios.AnyAsync(u => u.Id != id && u.Email.ToLower() == newEmail, cancellationToken);
            if (existeEmail)
            {
                return (null, "El correo electrónico ya está en uso por otro usuario.");
            }
            usuario.Email = newEmail;
            if (usuario.Cliente != null)
            {
                usuario.Cliente.Email = newEmail;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Rol))
        {
            usuario.Rol = request.Rol.Trim().ToLowerInvariant();
        }

        if (request.Activo.HasValue)
        {
            usuario.Activo = request.Activo.Value;
            if (usuario.Cliente != null)
            {
                usuario.Cliente.Activo = request.Activo.Value;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return (MapToUsuarioResponse(usuario, usuario.Cliente), null);
    }

    public async Task<bool> CambiarEstadoUsuarioAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Cliente)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario is null) return false;

        usuario.Activo = activo;
        if (usuario.Cliente != null)
        {
            usuario.Cliente.Activo = activo;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static UsuarioResponse MapToUsuarioResponse(Usuario u, Cliente? cliente)
    {
        return new UsuarioResponse(
            u.Id,
            u.Email,
            u.Rol,
            u.Activo,
            u.ClienteId,
            u.EmpleadoId,
            u.UltimoAcceso,
            cliente is null
                ? null
                : new ClienteResponse(
                    cliente.Id,
                    cliente.Nombre,
                    cliente.Apellido,
                    cliente.Email,
                    cliente.Telefono));
    }
}

