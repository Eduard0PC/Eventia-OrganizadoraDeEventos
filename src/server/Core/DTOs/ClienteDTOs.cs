using System;
using System.Collections.Generic;

namespace Server.Core.DTOs;

public class ClienteSummaryResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    public string Email { get; set; } = null!;
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public string EstatusLabel => Activo ? "Activo" : "Inactivo";
    public string UltimoEventoContratado { get; set; } = "Sin eventos contratados";
    public string? FechaUltimoEvento { get; set; }
    public int TotalEventos { get; set; }
}

public class ClienteDetalleResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    public string Email { get; set; } = null!;
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public string EstatusLabel => Activo ? "Activo" : "Inactivo";
    public string UltimoEventoContratado { get; set; } = "Sin eventos contratados";
    public int TotalEventos { get; set; }
    public decimal TotalPagado { get; set; }

    public List<PagoClienteDto> HistorialPagos { get; set; } = new();
    public List<ServicioContratadoDto> ServiciosContratados { get; set; } = new();
    public List<EventoActivoDto> EventosActivos { get; set; } = new();
}

public class PagoClienteDto
{
    public int Id { get; set; }
    public string FolioContrato { get; set; } = null!;
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = null!;
    public string TipoTransaccion { get; set; } = null!;
    public string FechaPago { get; set; } = null!;
    public string Estatus { get; set; } = null!;
    public string? Referencia { get; set; }
}

public class ServicioContratadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!; // evento / servicio
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
    public string FechaCotizacion { get; set; } = null!;
    public string FolioCotizacion { get; set; } = null!;
}

public class EventoActivoDto
{
    public int Id { get; set; }
    public string NombreEvento { get; set; } = null!;
    public string FechaEvento { get; set; } = null!;
    public string? Lugar { get; set; }
    public int? Aforo { get; set; }
    public string Estatus { get; set; } = null!;
    public string FolioContrato { get; set; } = null!;
}

public class CrearClienteRequest
{
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? Password { get; set; }
}
