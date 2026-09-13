using System;

namespace Server.Core.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    int Id,
    string Email,
    string Rol,
    ClienteResponse? Cliente,
    EmpleadoResponse? Empleado
);

public record RegisterRequest(
    string Email,
    string Password,
    string Nombre,
    string Apellido,
    string? Telefono,
    string? Rol = "cliente"
);

public record UsuarioResponse(
    int Id,
    string Email,
    string Rol,
    bool Activo,
    int? ClienteId,
    int? EmpleadoId,
    DateTime? UltimoAcceso,
    ClienteResponse? Cliente,
    EmpleadoResponse? Empleado
);

public record ActualizarUsuarioRequest(
    string? Email,
    string? Rol,
    bool? Activo
);

public record CambiarEstadoUsuarioRequest(
    bool Activo
);

public record ClienteResponse(int Id, string Nombre, string Apellido, string Email, string? Telefono);

public record EmpleadoResponse(int Id, string Nombre, string Apellido, string Email, string? Telefono, string? Cargo);
