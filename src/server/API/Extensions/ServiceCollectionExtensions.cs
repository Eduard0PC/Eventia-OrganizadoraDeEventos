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
            connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Require;Trust Server Certificate=true;Timeout=5;Command Timeout=10";
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

    public static async Task EnsureDatabaseCastsAsync(this WebApplication app, bool throwOnError = false)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
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

                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'estatus_contrato') THEN
                        CREATE CAST (text AS estatus_contrato) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'estatus_contrato') THEN
                        CREATE CAST (varchar AS estatus_contrato) WITH INOUT AS IMPLICIT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'estatus_evento') THEN
                        CREATE CAST (text AS estatus_evento) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'estatus_evento') THEN
                        CREATE CAST (varchar AS estatus_evento) WITH INOUT AS IMPLICIT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'estatus_plan_pago') THEN
                        CREATE CAST (text AS estatus_plan_pago) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'estatus_plan_pago') THEN
                        CREATE CAST (varchar AS estatus_plan_pago) WITH INOUT AS IMPLICIT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'estatus_pago') THEN
                        CREATE CAST (text AS estatus_pago) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'estatus_pago') THEN
                        CREATE CAST (varchar AS estatus_pago) WITH INOUT AS IMPLICIT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'metodo_pago_tipo') THEN
                        CREATE CAST (text AS metodo_pago_tipo) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'metodo_pago_tipo') THEN
                        CREATE CAST (varchar AS metodo_pago_tipo) WITH INOUT AS IMPLICIT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'text' AND t.typname = 'tipo_transaccion_tipo') THEN
                        CREATE CAST (text AS tipo_transaccion_tipo) WITH INOUT AS IMPLICIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_cast JOIN pg_type s ON pg_cast.castsource = s.oid JOIN pg_type t ON pg_cast.casttarget = t.oid WHERE s.typname = 'varchar' AND t.typname = 'tipo_transaccion_tipo') THEN
                        CREATE CAST (varchar AS tipo_transaccion_tipo) WITH INOUT AS IMPLICIT;
                    END IF;
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnsureDatabaseCasts - Casts]: {ex.Message}");
            if (throwOnError) throw;
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE public.proveedores
                  ADD COLUMN IF NOT EXISTS capacidad_maxima INTEGER NULL;

                CREATE TABLE IF NOT EXISTS public.evento_proveedores (
                    id SERIAL PRIMARY KEY,
                    evento_contratado_id INTEGER NOT NULL REFERENCES public.eventos_contratados(id) ON DELETE CASCADE,
                    proveedor_id INTEGER NOT NULL REFERENCES public.proveedores(id) ON DELETE RESTRICT,
                    catalogo_servicio_id INTEGER NULL REFERENCES public.catalogo_servicios(id) ON DELETE SET NULL,
                    clave_requerimiento VARCHAR(100) NOT NULL,
                    nombre_requerimiento VARCHAR(200) NOT NULL,
                    categoria_proveedor VARCHAR(100) NOT NULL,
                    es_locacion BOOLEAN NOT NULL DEFAULT FALSE,
                    costo_acordado NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
                    notas TEXT NULL,
                    confirmado BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    CONSTRAINT uq_evento_requerimiento UNIQUE (evento_contratado_id, clave_requerimiento),
                    CONSTRAINT chk_costo_acordado_no_negativo CHECK (costo_acordado >= 0)
                );

                CREATE INDEX IF NOT EXISTS idx_evento_proveedores_evento ON public.evento_proveedores(evento_contratado_id);
                CREATE INDEX IF NOT EXISTS idx_evento_proveedores_proveedor ON public.evento_proveedores(proveedor_id);

                CREATE TABLE IF NOT EXISTS public.evento_tareas (
                    id SERIAL PRIMARY KEY,
                    evento_contratado_id INTEGER NOT NULL REFERENCES public.eventos_contratados(id) ON DELETE CASCADE,
                    titulo VARCHAR(250) NOT NULL,
                    completada BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                );

                CREATE INDEX IF NOT EXISTS idx_evento_tareas_evento ON public.evento_tareas(evento_contratado_id);
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnsureDatabaseCasts - Planificador DDL]: {ex.Message}");
            if (throwOnError) throw;
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO public.categorias_proveedores (nombre, descripcion, activo, created_at, updated_at)
                SELECT 'Locación', 'Salones, jardines, haciendas y espacios para eventos', true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM public.categorias_proveedores WHERE LOWER(nombre) IN ('locación', 'locacion')
                );

                UPDATE public.proveedores p
                SET capacidad_maxima = 250
                FROM public.categorias_proveedores c
                WHERE p.categoria_id = c.id
                  AND LOWER(c.nombre) IN ('locación', 'locacion')
                  AND p.capacidad_maxima IS NULL;

                INSERT INTO public.proveedores (categoria_id, nombre, contacto, email, telefono, direccion, rating, precio_promedio, capacidad_maxima, activo, created_at, updated_at)
                SELECT c.id, v.nombre, v.contacto, v.email, v.telefono, v.direccion, v.rating, v.precio_promedio, v.capacidad_maxima, true, NOW(), NOW()
                FROM (
                    SELECT id FROM public.categorias_proveedores WHERE LOWER(nombre) IN ('locación', 'locacion') ORDER BY id LIMIT 1
                ) c
                CROSS JOIN (VALUES
                    ('Jardín Botánico Los Olivos (Boutique)', 'Arq. Sofía Méndez', 'reservas@jardinlosolivos.mx', '81 2345 1101', 'Carretera Nacional Km 25', 5, '$$', 100),
                    ('Hacienda San Gabriel de las Palmas', 'Lic. Roberto Villarreal', 'eventos@haciendasangabriel.mx', '81 8890 2202', 'Valle Alto, Monterrey', 5, '$$$', 250),
                    ('Gran Salón Imperial & Centro de Convenciones', 'Lic. Elena Garza', 'contacto@gransalonimperial.mx', '81 9901 3303', 'San Pedro Garza García', 5, '$$$$', 600)
                ) AS v(nombre, contacto, email, telefono, direccion, rating, precio_promedio, capacidad_maxima)
                WHERE NOT EXISTS (
                    SELECT 1 FROM public.proveedores p WHERE LOWER(p.nombre) = LOWER(v.nombre) OR LOWER(p.email) = LOWER(v.email)
                );
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnsureDatabaseCasts - Seed Locaciones]: {ex.Message}");
            if (throwOnError) throw;
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
        services.AddScoped<IEventoService, EventoService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<IPlanificadorService, PlanificadorService>();

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
