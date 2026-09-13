using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;
using Server.Core.Entities;

namespace Server.Core.Interfaces;

public interface ICatalogoService
{
    // Eventos de Catálogo
    Task<List<CatalogoEvento>> GetCatalogoEventosAsync(bool incluirInactivos = false, CancellationToken cancellationToken = default);
    Task<CatalogoEvento?> GetCatalogoEventoByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(CatalogoEvento? Result, string? Error)> CrearCatalogoEventoAsync(CrearCatalogoEventoRequest request, CancellationToken cancellationToken = default);
    Task<(CatalogoEvento? Result, string? Error)> ActualizarCatalogoEventoAsync(int id, ActualizarCatalogoEventoRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCatalogoEventoAsync(int id, CancellationToken cancellationToken = default);

    // Servicios de Catálogo
    Task<List<CatalogoServicio>> GetCatalogoServiciosAsync(CancellationToken cancellationToken = default);
    Task<CatalogoServicio?> GetCatalogoServicioByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(CatalogoServicio? Result, string? Error)> CrearCatalogoServicioAsync(CrearCatalogoServicioRequest request, CancellationToken cancellationToken = default);
    Task<(CatalogoServicio? Result, string? Error)> ActualizarCatalogoServicioAsync(int id, ActualizarCatalogoServicioRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCatalogoServicioAsync(int id, CancellationToken cancellationToken = default);
}


