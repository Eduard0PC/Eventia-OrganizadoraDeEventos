using Microsoft.EntityFrameworkCore;

// Load environment variables from .env file if it exists
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(envPath))
{
    envPath = Path.Combine(AppContext.BaseDirectory, ".env");
}

if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
        
        var parts = trimmed.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var value = parts[1].Trim();
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
            {
                value = value.Substring(1, value.Length - 2);
            }
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Build connection string from environment variables or fall back to appsettings.json
var dbHost = builder.Configuration["DB_HOST"] ?? Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = builder.Configuration["DB_PORT"] ?? Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = builder.Configuration["DB_NAME"] ?? Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
var dbUser = builder.Configuration["DB_USER"] ?? Environment.GetEnvironmentVariable("DB_USER");
var dbPass = builder.Configuration["DB_PASSWORD"] ?? Environment.GetEnvironmentVariable("DB_PASSWORD");

string? connectionString;
if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbUser))
{
    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Require;Trust Server Certificate=true;Timeout=10;Command Timeout=10";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.CommandTimeout(10)));

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? Array.Empty<string>();

    options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AngularClient");
app.UseHttpsRedirection();

app.MapGet("/api/catalogo-eventos", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var eventos = await db.CatalogoEventos
        .Where(e => e.Activo)
        .OrderBy(e => e.Nombre)
        .ToListAsync(cancellationToken);
    return Results.Ok(eventos);
})
.WithName("GetCatalogoEventos");

app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var email = request.Email.Trim().ToLowerInvariant();

    var usuario = await db.Usuarios
        .Include(u => u.Cliente)
        .FirstOrDefaultAsync(u =>
            u.Email.ToLower() == email &&
            u.Activo &&
            u.ClienteId != null &&
            u.Cliente != null &&
            u.Cliente.Activo,
            cancellationToken);

    if (usuario is null || !PasswordMatches(request.Password, usuario.PasswordHash))
    {
        return Results.Unauthorized();
    }

    usuario.UltimoAcceso = DateTime.UtcNow;
    await db.SaveChangesAsync(cancellationToken);

    return Results.Ok(new LoginResponse(
        usuario.Id,
        usuario.Email,
        usuario.Rol,
        usuario.Cliente is null
            ? null
            : new ClienteResponse(
                usuario.Cliente.Id,
                usuario.Cliente.Nombre,
                usuario.Cliente.Apellido,
                usuario.Cliente.Email,
                usuario.Cliente.Telefono)));
})
.WithName("Login");

app.MapGet("/api/cotizaciones", async (int? clienteId, AppDbContext db, CancellationToken cancellationToken) =>
{
    var query = db.Cotizaciones
        .Include(c => c.Cliente)
        .Include(c => c.Items)
            .ThenInclude(i => i.CatalogoEvento)
        .AsQueryable();

    if (clienteId.HasValue && clienteId.Value > 0)
    {
        query = query.Where(c => c.ClienteId == clienteId.Value);
    }

    var cotizaciones = await query
        .OrderByDescending(c => c.CreatedAt)
        .ToListAsync(cancellationToken);

    var dtos = cotizaciones.Select(c =>
    {
        var primerItemEvento = c.Items.FirstOrDefault(i => i.Tipo == "evento");
        var nombreEvento = primerItemEvento?.CatalogoEvento?.Nombre ?? "Evento personalizado";

        var itemsDto = c.Items.Select(i => new CotizacionItemDto(
            i.Id,
            !string.IsNullOrWhiteSpace(i.Notas) ? i.Notas : (i.CatalogoEvento != null ? $"Paquete {i.CatalogoEvento.Nombre}" : "Servicio"),
            i.Subtotal ?? (i.Cantidad * i.PrecioUnitario - i.DescuentoItem)
        )).ToList();

        return new CotizacionResponse(
            c.Id,
            c.Folio,
            nombreEvento,
            primerItemEvento?.CatalogoEventoId,
            c.FechaEvento?.ToString("yyyy-MM-dd"),
            c.Invitados,
            c.Total,
            c.Descuento,
            c.TotalFinal ?? (c.Total - c.Descuento),
            c.Estatus,
            c.FechaVigencia.ToString("yyyy-MM-dd"),
            c.CreatedAt.ToString("yyyy-MM-dd"),
            c.Notas ?? "",
            itemsDto
        );
    }).ToList();

    return Results.Ok(dtos);
})
.WithName("GetCotizaciones");

app.MapGet("/api/cotizaciones/{id:int}", async (int id, AppDbContext db, CancellationToken cancellationToken) =>
{
    var c = await db.Cotizaciones
        .Include(c => c.Cliente)
        .Include(c => c.Items)
            .ThenInclude(i => i.CatalogoEvento)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    if (c is null) return Results.NotFound();

    var primerItemEvento = c.Items.FirstOrDefault(i => i.Tipo == "evento");
    var nombreEvento = primerItemEvento?.CatalogoEvento?.Nombre ?? "Evento personalizado";

    var itemsDto = c.Items.Select(i => new CotizacionItemDto(
        i.Id,
        i.Tipo == "evento" && i.CatalogoEvento != null ? $"Paquete {i.CatalogoEvento.Nombre}" : (i.Notas ?? "Servicio"),
        i.Subtotal ?? (i.Cantidad * i.PrecioUnitario - i.DescuentoItem)
    )).ToList();

    var dto = new CotizacionResponse(
        c.Id,
        c.Folio,
        nombreEvento,
        primerItemEvento?.CatalogoEventoId,
        c.FechaEvento?.ToString("yyyy-MM-dd"),
        c.Invitados,
        c.Total,
        c.Descuento,
        c.TotalFinal ?? (c.Total - c.Descuento),
        c.Estatus,
        c.FechaVigencia.ToString("yyyy-MM-dd"),
        c.CreatedAt.ToString("yyyy-MM-dd"),
        c.Notas ?? "",
        itemsDto
    );

    return Results.Ok(dto);
})
.WithName("GetCotizacionById");

app.MapPost("/api/cotizaciones", async (CrearCotizacionRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var evento = await db.CatalogoEventos.FindAsync(new object[] { request.CatalogoEventoId }, cancellationToken);
    if (evento is null)
    {
        return Results.BadRequest(new { error = "El evento especificado no existe en el catálogo." });
    }

    int targetClienteId = request.ClienteId ?? 0;
    if (targetClienteId <= 0)
    {
        var primerCliente = await db.Clientes.FirstOrDefaultAsync(c => c.Activo, cancellationToken);
        if (primerCliente != null)
        {
            targetClienteId = primerCliente.Id;
        }
        else
        {
            return Results.BadRequest(new { error = "No hay clientes activos registrados." });
        }
    }

    decimal totalBase = evento.PrecioBase;
    decimal extraHorasMonto = request.HorasAdicionales > 0 ? request.HorasAdicionales * 1000m : 0m;
    decimal totalCotizacion = totalBase + extraHorasMonto;

    // Generar un folio único: COT-YYYY-XXXX
    int randomNum = Random.Shared.Next(1000, 9999);
    string folio = $"COT-{DateTime.UtcNow.Year}-{randomNum}";

    DateTime? fechaEventoParsed = null;
    if (DateTime.TryParse(request.FechaEvento, out var parsedDate))
    {
        fechaEventoParsed = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
    }

    var nuevaCotizacion = new Cotizacion
    {
        ClienteId = targetClienteId,
        Folio = folio,
        Total = totalCotizacion,
        Descuento = 0m,
        Estatus = "enviada",
        FechaVigencia = DateTime.UtcNow.AddDays(30),
        FechaEvento = fechaEventoParsed,
        Invitados = request.Invitados,
        Notas = request.Notas,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Cotizaciones.Add(nuevaCotizacion);
    await db.SaveChangesAsync(cancellationToken);

    var itemEvento = new CotizacionItem
    {
        CotizacionId = nuevaCotizacion.Id,
        Tipo = "evento",
        Cantidad = 1,
        PrecioUnitario = evento.PrecioBase,
        DescuentoItem = 0m,
        Notas = $"Paquete {evento.Nombre} - Invitados: {request.Invitados}" + (request.HorasAdicionales > 0 ? $", Horas extra: {request.HorasAdicionales}" : ""),
        CatalogoEventoId = evento.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.CotizacionItems.Add(itemEvento);

    if (request.HorasAdicionales > 0)
    {
        var itemHoras = new CotizacionItem
        {
            CotizacionId = nuevaCotizacion.Id,
            Tipo = "evento",
            Cantidad = (short)request.HorasAdicionales,
            PrecioUnitario = 1000m,
            DescuentoItem = 0m,
            Notas = $"Horas adicionales ({request.HorasAdicionales} hrs)",
            CatalogoEventoId = evento.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.CotizacionItems.Add(itemHoras);
    }

    await db.SaveChangesAsync(cancellationToken);

    var itemsDto = new List<CotizacionItemDto>
    {
        new CotizacionItemDto(itemEvento.Id, $"Paquete {evento.Nombre}", evento.PrecioBase)
    };
    if (request.HorasAdicionales > 0)
    {
        itemsDto.Add(new CotizacionItemDto(0, $"Horas adicionales ({request.HorasAdicionales} hrs)", extraHorasMonto));
    }

    var responseDto = new CotizacionResponse(
        nuevaCotizacion.Id,
        nuevaCotizacion.Folio,
        evento.Nombre,
        evento.Id,
        nuevaCotizacion.FechaEvento?.ToString("yyyy-MM-dd"),
        nuevaCotizacion.Invitados,
        nuevaCotizacion.Total,
        nuevaCotizacion.Descuento,
        nuevaCotizacion.Total,
        nuevaCotizacion.Estatus,
        nuevaCotizacion.FechaVigencia.ToString("yyyy-MM-dd"),
        nuevaCotizacion.CreatedAt.ToString("yyyy-MM-dd"),
        nuevaCotizacion.Notas ?? "",
        itemsDto
    );

    return Results.Created($"/api/cotizaciones/{nuevaCotizacion.Id}", responseDto);
})
.WithName("CrearCotizacion");

app.MapDelete("/api/cotizaciones/{id:int}", async (int id, AppDbContext db, CancellationToken cancellationToken) =>
{
    var cotizacion = await db.Cotizaciones.FindAsync(new object[] { id }, cancellationToken);
    if (cotizacion is null) return Results.NotFound();

    db.Cotizaciones.Remove(cotizacion);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
})
.WithName("DeleteCotizacion");

app.Run();

static bool PasswordMatches(string password, string passwordHash)
{
    try
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
    catch
    {
        return false;
    }
}

record LoginRequest(string Email, string Password);
record LoginResponse(int Id, string Email, string Rol, ClienteResponse? Cliente);
record ClienteResponse(int Id, string Nombre, string Apellido, string Email, string? Telefono);

record CotizacionItemDto(int Id, string Descripcion, decimal Monto);
record CotizacionResponse(
    int Id,
    string Folio,
    string Evento,
    int? CatalogoEventoId,
    string? FechaEvento,
    int Invitados,
    decimal Total,
    decimal Descuento,
    decimal TotalFinal,
    string Estatus,
    string FechaVigencia,
    string FechaCreacion,
    string Notas,
    List<CotizacionItemDto> Items
);
record CrearCotizacionRequest(
    int? ClienteId,
    int CatalogoEventoId,
    string FechaEvento,
    int Invitados,
    int HorasAdicionales,
    string? Notas
);

