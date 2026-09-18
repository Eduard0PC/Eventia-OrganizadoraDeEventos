using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;
using Server.Infrastructure.Services;

namespace Server.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbHost = configuration["DB_HOST"] ?? Environment.GetEnvironmentVariable("DB_HOST");
        var dbPort = configuration["DB_PORT"] ?? Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = configuration["DB_NAME"] ?? Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
        var dbUser = configuration["DB_USER"] ?? Environment.GetEnvironmentVariable("DB_USER");
        var dbPass = configuration["DB_PASSWORD"] ?? Environment.GetEnvironmentVariable("DB_PASSWORD");

        string? connectionString;
        if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbUser))
        {
            connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Require;Trust Server Certificate=true;Timeout=10;Command Timeout=10";
        }
        else
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.CommandTimeout(10)));

        return services;
    }

    public static async Task EnsureDatabaseCastsAsync(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'rol_usuario') THEN
                        CREATE CAST (text AS rol_usuario) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'rol_usuario') THEN
                        CREATE CAST (varchar AS rol_usuario) WITH INOUT AS IMPLICIT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'estatus_cotizacion') THEN
                        CREATE CAST (text AS estatus_cotizacion) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'estatus_cotizacion') THEN
                        CREATE CAST (varchar AS estatus_cotizacion) WITH INOUT AS IMPLICIT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'tipo_item_cotizacion') THEN
                        CREATE CAST (text AS tipo_item_cotizacion) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'tipo_item_cotizacion') THEN
                        CREATE CAST (varchar AS tipo_item_cotizacion) WITH INOUT AS IMPLICIT;
                    END IF;
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnsureDatabaseCasts]: {ex.Message}");
        }
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<ICotizacionService, CotizacionService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IProveedorService, ProveedorService>();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()
                ?? Array.Empty<string>();

            options.AddPolicy("AngularClient", policy =>
            {
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }
}
