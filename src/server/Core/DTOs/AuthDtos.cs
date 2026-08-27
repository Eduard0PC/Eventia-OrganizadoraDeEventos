namespace Server.Core.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(int Id, string Email, string Rol, ClienteResponse? Cliente);

public record ClienteResponse(int Id, string Nombre, string Apellido, string Email, string? Telefono);
