using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Server.Core.Entities;
using Server.Infrastructure.Data;
using Server.Infrastructure.Services;
using Xunit;

namespace Server.Tests.Services;

public class CatalogoServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task ListarServicios_RetornaActivosYOrdenados()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.CatalogoServicios.AddRange(new List<CatalogoServicio>
        {
            new() { Id = 1, Nombre = "Z - Sonido e Iluminación", Activo = true, PrecioBase = 5000 },
            new() { Id = 2, Nombre = "A - Banquete de Gala", Activo = true, PrecioBase = 12000 },
            new() { Id = 3, Nombre = "M - Mobiliario Vintage (Inactivo)", Activo = false, PrecioBase = 3000 }
        });
        await context.SaveChangesAsync();

        var service = new CatalogoService(context);

        // Act
        var result = await service.GetCatalogoServiciosAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().NotContain(s => s.Activo == false);
        result.Select(s => s.Nombre).Should().ContainInOrder(
            "A - Banquete de Gala",
            "Z - Sonido e Iluminación"
        );
    }

    [Fact]
    public async Task ListarServicios_SinDatos_RetornaVacio()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogoService(context);

        // Act
        var result = await service.GetCatalogoServiciosAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarPorId_Existe_RetornaServicio()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var servicio = new CatalogoServicio
        {
            Id = 10,
            Nombre = "Fotografía y Video HD",
            PrecioBase = 8500,
            Activo = true
        };
        context.CatalogoServicios.Add(servicio);
        await context.SaveChangesAsync();

        var service = new CatalogoService(context);

        // Act
        var result = await service.GetCatalogoServicioByIdAsync(10);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(10);
        result.Nombre.Should().Be("Fotografía y Video HD");
        result.PrecioBase.Should().Be(8500);
    }

    [Fact]
    public async Task BuscarPorId_NoExiste_RetornaNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogoService(context);

        // Act
        var result = await service.GetCatalogoServicioByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ServiciosExtra_PorCategoria_FiltraCorrectamente()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.CatalogoServicios.AddRange(new List<CatalogoServicio>
        {
            // Caso 1: Categoría que coincide: DEBE INCLUIRSE
            new() { Id = 1, Nombre = "Mesa de Dulces", Categoria = "Sociales", Activo = true, VisibleCliente = true },

            // Caso 2: Categoría NULL (aplica a todos los eventos): DEBE INCLUIRSE
            new() { Id = 2, Nombre = "Hora Extra de Salón", Categoria = null, Activo = true, VisibleCliente = true },

            // Caso 3: Otra categoría ("Corporativos"): NO DEBE INCLUIRSE
            new() { Id = 3, Nombre = "Traductor Simultáneo", Categoria = "Corporativos", Activo = true, VisibleCliente = true },

            // Caso 4: Categoría coincide pero está inactivo: NO DEBE INCLUIRSE
            new() { Id = 5, Nombre = "Mariachi Descontinuado", Categoria = "Sociales", Activo = false, VisibleCliente = true }
        });
        await context.SaveChangesAsync();

        var service = new CatalogoService(context);

        // Act
        var result = await service.GetServiciosExtraAsync("Sociales");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Nombre == "Mesa de Dulces");
        result.Should().Contain(s => s.Nombre == "Hora Extra de Salón");
        result.Should().NotContain(s => s.Nombre == "Traductor Simultáneo");
        result.Should().NotContain(s => s.Nombre == "Mariachi Descontinuado");
    }
}

