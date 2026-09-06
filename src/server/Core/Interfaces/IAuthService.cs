using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;

namespace Server.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<(UsuarioResponse? Result, string? Error)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<UsuarioResponse>> GetUsuariosAsync(CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> GetUsuarioByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(UsuarioResponse? Result, string? Error)> ActualizarUsuarioAsync(int id, ActualizarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<bool> CambiarEstadoUsuarioAsync(int id, bool activo, CancellationToken cancellationToken = default);
}

