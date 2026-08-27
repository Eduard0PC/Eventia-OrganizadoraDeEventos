using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Core.DTOs;
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
}
