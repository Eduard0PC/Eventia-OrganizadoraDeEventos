using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Server.Core.DTOs;
using Server.Core.Entities;
using Server.Infrastructure.Data;
using Server.Infrastructure.Services;
using Xunit;

namespace Server.Tests.Services;

public class PlanificadorServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"PlanificadorTestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task LocacionesDisponibles_FiltraPorCapacidadSegunAforoDelEvento()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var catLocacion = new CategoriaProveedor { Id = 1, Nombre = "Locación", Activo = true };
        context.CategoriasProveedores.Add(catLocacion);

        context.Proveedores.AddRange(
            new Proveedor { Id = 10, CategoriaId = 1, Nombre = "Salón Pequeño", CapacidadMaxima = 80, Activo = true },
            new Proveedor { Id = 11, CategoriaId = 1, Nombre = "Hacienda Mediana", CapacidadMaxima = 200, Activo = true },
            new Proveedor { Id = 12, CategoriaId = 1, Nombre = "Centro de Convenciones Grande", CapacidadMaxima = 500, Activo = true }
        );

        var cliente = new Cliente { Id = 1, Nombre = "Ana", Apellido = "López", Email = "ana@test.com", Activo = true };
        var catEvento = new CatalogoEvento { Id = 1, Nombre = "Boda Real", Categoria = "Bodas", PrecioBase = 45000, CapacidadPersonas = 150, Activo = true };
        var contrato = new Contrato { Id = 1, ClienteId = 1, Folio = "CTR-2026-0001", TotalContrato = 55000, FechaInicio = new DateTime(2026, 12, 10), FechaFin = new DateTime(2026, 12, 10) };
        var evento = new EventoContratado
        {
            Id = 100,
            ContratoId = 1,
            CatalogoEventoId = 1,
            FechaEvento = new DateTime(2026, 12, 10),
            Aforo = 180,
            Estatus = "programado"
        };

        context.Clientes.Add(cliente);
        context.CatalogoEventos.Add(catEvento);
        context.Contratos.Add(contrato);
        context.EventosContratados.Add(evento);
        await context.SaveChangesAsync();

        var service = new PlanificadorService(context);

        // Act
        var disponibles = await service.GetProveedoresDisponiblesParaEventoAsync(100, "Locación", esLocacion: true);

        // Assert: Solo deben aparecer las locaciones con capacidad >= 180 personas
        disponibles.Should().HaveCount(2);
        disponibles.Should().Contain(p => p.Nombre == "Hacienda Mediana");
        disponibles.Should().Contain(p => p.Nombre == "Centro de Convenciones Grande");
        disponibles.Should().NotContain(p => p.Nombre == "Salón Pequeño");
    }

    [Fact]
    public async Task AsignarProveedor_BloqueaFechaYNoApareceDisponibleEnOtroEventoMismaFecha()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var catFoto = new CategoriaProveedor { Id = 2, Nombre = "Fotografía", Activo = true };
        var proveedorFoto = new Proveedor { Id = 20, CategoriaId = 2, Nombre = "Studio Lente Dorado", Activo = true };
        var cliente = new Cliente { Id = 1, Nombre = "Carlos", Apellido = "Ruiz", Email = "carlos@test.com", Activo = true };
        var catEvento = new CatalogoEvento { Id = 1, Nombre = "Graduación Gala", Categoria = "Graduaciones", PrecioBase = 40000, CapacidadPersonas = 100, Activo = true };

        var fechaCompartida = new DateTime(2026, 11, 28);
        var otraFecha = new DateTime(2026, 11, 29);

        var contrato1 = new Contrato { Id = 1, ClienteId = 1, Folio = "CTR-2026-0001", TotalContrato = 40000, FechaInicio = fechaCompartida, FechaFin = fechaCompartida };
        var contrato2 = new Contrato { Id = 2, ClienteId = 1, Folio = "CTR-2026-0002", TotalContrato = 45000, FechaInicio = fechaCompartida, FechaFin = fechaCompartida };
        var contrato3 = new Contrato { Id = 3, ClienteId = 1, Folio = "CTR-2026-0003", TotalContrato = 45000, FechaInicio = otraFecha, FechaFin = otraFecha };

        var evento1 = new EventoContratado { Id = 1, ContratoId = 1, CatalogoEventoId = 1, FechaEvento = fechaCompartida, Aforo = 100, Estatus = "programado" };
        var evento2MismaFecha = new EventoContratado { Id = 2, ContratoId = 2, CatalogoEventoId = 1, FechaEvento = fechaCompartida, Aforo = 100, Estatus = "programado" };
        var evento3OtraFecha = new EventoContratado { Id = 3, ContratoId = 3, CatalogoEventoId = 1, FechaEvento = otraFecha, Aforo = 100, Estatus = "programado" };

        context.CategoriasProveedores.Add(catFoto);
        context.Proveedores.Add(proveedorFoto);
        context.Clientes.Add(cliente);
        context.CatalogoEventos.Add(catEvento);
        context.Contratos.AddRange(contrato1, contrato2, contrato3);
        context.EventosContratados.AddRange(evento1, evento2MismaFecha, evento3OtraFecha);
        await context.SaveChangesAsync();

        var service = new PlanificadorService(context);

        // Act 1: Asignar proveedor al evento 1
        var (plan1, error1) = await service.AsignarProveedorAsync(1, new AsignarProveedorEventoRequest(
            ClaveRequerimiento: "SRV-FOTO",
            NombreRequerimiento: "Fotografía Profesional",
            CategoriaProveedor: "Fotografía",
            ProveedorId: 20,
            CostoAcordado: 6500m
        ));

        // Assert 1: Asignación exitosa
        error1.Should().BeNull();
        plan1.Should().NotBeNull();

        // Act 2: Consultar proveedores disponibles para Evento 2 (misma fecha)
        var disponiblesMismaFecha = await service.GetProveedoresDisponiblesParaEventoAsync(2, "Fotografía");
        disponiblesMismaFecha.Should().BeEmpty("el proveedor ya está bloqueado en esa fecha");

        // Act 3: Intentar forzar asignación en Evento 2 (misma fecha) debe fallar desde el backend
        var (plan2, error2) = await service.AsignarProveedorAsync(2, new AsignarProveedorEventoRequest(
            ClaveRequerimiento: "SRV-FOTO",
            NombreRequerimiento: "Fotografía Profesional",
            CategoriaProveedor: "Fotografía",
            ProveedorId: 20,
            CostoAcordado: 6500m
        ));
        plan2.Should().BeNull();
        error2.Should().NotBeNullOrEmpty();

        // Act 4: Consultar proveedores disponibles para Evento 3 (otra fecha)
        var disponiblesOtraFecha = await service.GetProveedoresDisponiblesParaEventoAsync(3, "Fotografía");
        disponiblesOtraFecha.Should().ContainSingle(p => p.Id == 20);
    }

    [Fact]
    public async Task CalculoMargenGanancia_Y_ChecklistLibre_FuncionanCorrectamente()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var catLoc = new CategoriaProveedor { Id = 1, Nombre = "Locación", Activo = true };
        var catCat = new CategoriaProveedor { Id = 2, Nombre = "Catering", Activo = true };
        context.CategoriasProveedores.AddRange(catLoc, catCat);

        context.Proveedores.AddRange(
            new Proveedor { Id = 1, CategoriaId = 1, Nombre = "Hacienda Real", CapacidadMaxima = 250, Activo = true },
            new Proveedor { Id = 2, CategoriaId = 2, Nombre = "Banquetes Gourmet", Activo = true }
        );

        var cliente = new Cliente { Id = 1, Nombre = "Luis", Apellido = "Gómez", Email = "luis@test.com", Activo = true };
        var catEvento = new CatalogoEvento { Id = 1, Nombre = "Boda Imperial", Categoria = "Bodas", PrecioBase = 60000, CapacidadPersonas = 200, Activo = true };
        var contrato = new Contrato { Id = 1, ClienteId = 1, Folio = "CTR-2026-0010", TotalContrato = 80000m, FechaInicio = new DateTime(2026, 12, 20), FechaFin = new DateTime(2026, 12, 20) };
        var evento = new EventoContratado { Id = 10, ContratoId = 1, CatalogoEventoId = 1, FechaEvento = new DateTime(2026, 12, 20), Aforo = 200, Estatus = "programado" };

        context.Clientes.Add(cliente);
        context.CatalogoEventos.Add(catEvento);
        context.Contratos.Add(contrato);
        context.EventosContratados.Add(evento);
        await context.SaveChangesAsync();

        var service = new PlanificadorService(context);

        // Act: Asignar Locación ($20,000) y Catering ($30,000) -> Costo total $50,000 sobre Contrato de $80,000
        await service.AsignarProveedorAsync(10, new AsignarProveedorEventoRequest(
            ClaveRequerimiento: "LOCACION",
            NombreRequerimiento: "Locación Principal",
            CategoriaProveedor: "Locación",
            ProveedorId: 1,
            CostoAcordado: 20000m,
            EsLocacion: true
        ));

        await service.AsignarProveedorAsync(10, new AsignarProveedorEventoRequest(
            ClaveRequerimiento: "BASE-CATERING",
            NombreRequerimiento: "Banquete Base",
            CategoriaProveedor: "Catering",
            ProveedorId: 2,
            CostoAcordado: 30000m
        ));

        // Agregar tarea personalizada del checklist libre
        var (tareaCreada, errTarea) = await service.CrearTareaAsync(10, new CrearEventoTareaRequest("Montar mesa de dulces sin gluten solicitada en notas"));
        errTarea.Should().BeNull();
        tareaCreada.Should().NotBeNull();

        await service.ToggleTareaAsync(10, tareaCreada!.Id);

        var planificacion = await service.GetPlanificacionByEventoIdAsync(10);

        // Assert
        planificacion.Should().NotBeNull();
        planificacion!.Lugar.Should().Be("Hacienda Real");
        planificacion.TotalContrato.Should().Be(80000m);
        planificacion.CostoTotalProveedores.Should().Be(50000m);
        planificacion.MargenGanancia.Should().Be(30000m);
        planificacion.PorcentajeMargen.Should().Be(37.5m);
        planificacion.Checklist.Should().ContainSingle(t => t.Titulo.Contains("sin gluten") && t.Completada);
    }
}

