using System.Collections.Generic;

namespace Server.Core.DTOs;

public record ProveedorAsignadoDto(
    int Id,
    int ProveedorId,
    string ProveedorNombre,
    string? ProveedorContacto,
    string? ProveedorTelefono,
    string? ProveedorEmail,
    string? ProveedorDireccion,
    int Rating,
    string PrecioPromedio,
    int? CapacidadMaxima,
    decimal CostoAcordado,
    string? Notas,
    bool Confirmado,
    string FechaAsignacion
);

public record RequerimientoProveedorDto(
    string ClaveRequerimiento,
    string NombreRequerimiento,
    string? Descripcion,
    string CategoriaProveedor,
    string TipoOrigen,
    bool EsLocacion,
    int? AforoMinimo,
    int? CatalogoServicioId,
    decimal PrecioCobradoCliente,
    ProveedorAsignadoDto? Asignacion
);

public record EventoTareaDto(
    int Id,
    int EventoContratadoId,
    string Titulo,
    bool Completada,
    string CreatedAt
);

public record PlanificacionEventoDto(
    int EventoContratadoId,
    int ContratoId,
    string FolioContrato,
    string NombreEvento,
    string CategoriaEvento,
    int ClienteId,
    string ClienteNombre,
    string? ClienteTelefono,
    string FechaEvento,
    string HoraInicio,
    string HoraFin,
    string Lugar,
    int Aforo,
    string EstatusEvento,
    string? NotasCliente,
    decimal TotalContrato,
    decimal CostoTotalProveedores,
    decimal MargenGanancia,
    decimal PorcentajeMargen,
    int TotalRequerimientos,
    int RequerimientosCubiertos,
    int Progreso,
    List<RequerimientoProveedorDto> Requerimientos,
    List<EventoTareaDto> Checklist
);

public record ProveedorDisponibleDto(
    int Id,
    int CategoriaId,
    string CategoriaNombre,
    string Nombre,
    string? Contacto,
    string? Telefono,
    string? Email,
    string? Direccion,
    int Rating,
    string PrecioPromedio,
    int? CapacidadMaxima,
    decimal CostoSugerido,
    List<ProveedorServicioDto> Servicios
);

public record AsignarProveedorEventoRequest(
    string ClaveRequerimiento,
    string NombreRequerimiento,
    string CategoriaProveedor,
    int ProveedorId,
    decimal CostoAcordado,
    int? CatalogoServicioId = null,
    bool EsLocacion = false,
    string? Notas = null
);

public record CrearEventoTareaRequest(
    string Titulo
);

