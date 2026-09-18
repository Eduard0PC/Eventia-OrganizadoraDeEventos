using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Core.DTOs;

namespace Server.Core.Interfaces;

public interface IProveedorService
{
    Task<List<ProveedorResponseDto>> GetProveedoresAsync(int? categoriaId = null, bool incluirInactivos = false, CancellationToken cancellationToken = default);
    Task<ProveedorResponseDto?> GetProveedorByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(ProveedorResponseDto? Result, string? Error)> CrearProveedorAsync(CrearProveedorRequest request, CancellationToken cancellationToken = default);
    Task<(ProveedorResponseDto? Result, string? Error)> ActualizarProveedorAsync(int id, ActualizarProveedorRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProveedorAsync(int id, CancellationToken cancellationToken = default);

    // Categorías
    Task<List<CategoriaProveedorDto>> GetCategoriasAsync(bool incluirInactivos = false, CancellationToken cancellationToken = default);

    // Servicios del proveedor
    Task<List<ProveedorServicioDto>> GetServiciosByProveedorIdAsync(int proveedorId, CancellationToken cancellationToken = default);
    Task<(ProveedorServicioDto? Result, string? Error)> CrearServicioAsync(int proveedorId, CrearProveedorServicioRequest request, CancellationToken cancellationToken = default);
    Task<(ProveedorServicioDto? Result, string? Error)> ActualizarServicioAsync(int id, ActualizarProveedorServicioRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteServicioAsync(int id, CancellationToken cancellationToken = default);
}
