using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;

namespace Server.Core.Interfaces;

public interface IPlanificadorService
{
    Task<List<PlanificacionEventoDto>> GetPlanificacionesAsync(CancellationToken cancellationToken = default);
    Task<PlanificacionEventoDto?> GetPlanificacionByEventoIdAsync(int eventoContratadoId, CancellationToken cancellationToken = default);
    Task<List<ProveedorDisponibleDto>> GetProveedoresDisponiblesParaEventoAsync(
        int eventoContratadoId,
        string? categoriaProveedor,
        bool esLocacion = false,
        CancellationToken cancellationToken = default);
    Task<(PlanificacionEventoDto? Result, string? Error)> AsignarProveedorAsync(
        int eventoContratadoId,
        AsignarProveedorEventoRequest request,
        CancellationToken cancellationToken = default);
    Task<(PlanificacionEventoDto? Result, string? Error)> RemoverAsignacionProveedorAsync(
        int eventoContratadoId,
        int asignacionId,
        CancellationToken cancellationToken = default);
    Task<(EventoTareaDto? Result, string? Error)> CrearTareaAsync(
        int eventoContratadoId,
        CrearEventoTareaRequest request,
        CancellationToken cancellationToken = default);
    Task<(EventoTareaDto? Result, string? Error)> ToggleTareaAsync(
        int eventoContratadoId,
        int tareaId,
        CancellationToken cancellationToken = default);
    Task<bool> EliminarTareaAsync(
        int eventoContratadoId,
        int tareaId,
        CancellationToken cancellationToken = default);
}

