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
