using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<ICotizacionService, CotizacionService>();

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
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        return services;
    }
}
