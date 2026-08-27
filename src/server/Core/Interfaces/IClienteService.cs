using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;

namespace Server.Core.Interfaces;

public interface IClienteService
{
    Task<List<ClienteSummaryResponse>> GetClientesAsync(CancellationToken cancellationToken = default);
    Task<ClienteDetalleResponse?> GetClienteFichaAsync(int id, CancellationToken cancellationToken = default);
    Task<(ClienteSummaryResponse? Result, string? Error)> CrearClienteAsync(CrearClienteRequest request, CancellationToken cancellationToken = default);
}
