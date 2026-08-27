using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;

namespace Server.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
