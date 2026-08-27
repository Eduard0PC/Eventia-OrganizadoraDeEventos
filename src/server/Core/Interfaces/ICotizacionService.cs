using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;

namespace Server.Core.Interfaces;

public interface ICotizacionService
{
    Task<List<CotizacionResponse>> GetCotizacionesAsync(int? clienteId, CancellationToken cancellationToken = default);
    Task<CotizacionResponse?> GetCotizacionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(CotizacionResponse? Result, string? Error)> CrearCotizacionAsync(CrearCotizacionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCotizacionAsync(int id, CancellationToken cancellationToken = default);
}
