using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;

namespace Server.Core.Interfaces;

public interface IPagoService
{
    Task<List<PagoItemDto>> GetPagosAsync(int? clienteId, int? contratoId, CancellationToken cancellationToken = default);
    Task<List<CuotaPendienteDto>> GetCuotasPendientesAsync(int? clienteId, int? contratoId, CancellationToken cancellationToken = default);
    Task<ResumenPagosDto> GetResumenAsync(int? clienteId, CancellationToken cancellationToken = default);
    Task<List<EstadoCuentaEventoDto>> GetEstadoCuentaAsync(int? clienteId, int? contratoId, CancellationToken cancellationToken = default);
    Task<(RegistrarPagoResponse? Result, string? Error)> RegistrarPagoAsync(RegistrarPagoRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> GenerarPlanPagosDefaultAsync(int contratoId, CancellationToken cancellationToken = default);
}
