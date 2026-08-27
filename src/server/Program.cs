using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Server.API.Endpoints;
using Server.API.Extensions;

// 1. Load environment variables from .env file
EnvironmentExtensions.LoadEnvironmentVariables();

var builder = WebApplication.CreateBuilder(args);

// 2. Configure Services
builder.Services.AddOpenApi();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddCorsPolicy(builder.Configuration);

var app = builder.Build();

// 3. Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AngularClient");
app.UseHttpsRedirection();

// 4. Map API Endpoints
app.MapAuthEndpoints();
app.MapCatalogoEndpoints();
app.MapCotizacionEndpoints();

app.Run();
