using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;

namespace Server.Core.Interfaces;

public interface IEventoService
{
    Task<List<EventoContratadoResponse>> GetEventosContratadosAsync(int? clienteId, CancellationToken cancellationToken = default);
    Task<EventoContratadoResponse?> GetEventoContratadoByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(EventoContratadoResponse? Result, string? Error)> ActualizarEstatusEventoAsync(int id, string estatus, string? notas, CancellationToken cancellationToken = default);
}
