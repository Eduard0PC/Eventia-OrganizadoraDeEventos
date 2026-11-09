using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Server.Core.DTOs;
using Server.Core.Entities;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;
using Server.Infrastructure.Services;
using Xunit;

namespace Server.Tests.Services;

public class AuthServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_EmailDuplicado_RetornaErrorEsperado()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        context.Usuarios.Add(new Usuario
        {
            Id = 1,
            Email = "duplicado@ejemplo.com",
            PasswordHash = hasher.HashPassword("123456"),
            Rol = "cliente",
            Activo = true
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, hasher);
        var request = new RegisterRequest("duplicado@ejemplo.com", "123456", "Juan", "Perez", "9991234567", "cliente");

        // Act
        var (result, error) = await service.RegisterAsync(request);

        // Assert
        result.Should().BeNull();
        error.Should().Be("El correo electrónico ya se encuentra registrado.");
    }

    [Fact]
    public async Task RegisterAsync_ErrorEnBaseDeDatos_RetornaMensajeGenerico()
    {
        // Arrange: DbContext disposed to force an exception
        var context = CreateInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        var service = new AuthService(context, hasher);
        context.Dispose();

        var request = new RegisterRequest("nuevo@ejemplo.com", "123456", "Juan", "Perez", "9991234567", "cliente");

        // Act
        var (result, error) = await service.RegisterAsync(request);

        // Assert
        result.Should().BeNull();
        error.Should().Be("imposible accesar en este momento, intentalo mas tarde");
    }

    [Fact]
    public async Task LoginAsync_CredencialesInvalidas_RetornaNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var hasher = new BcryptPasswordHasher();
        var service = new AuthService(context, hasher);
        var request = new LoginRequest("noexiste@ejemplo.com", "123456");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }
}
