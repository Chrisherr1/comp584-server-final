using Microsoft.EntityFrameworkCore;
using Comp584ServerFinal.Data.Models;   // adjust namespace to your DbContext

var builder = WebApplication.CreateBuilder(args);

// Swagger Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Ability to use Controllers
builder.Services.AddControllers();

// CORS for Angular dev server
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("NgDev", p => p
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// DbContext (Pomelo provider)
var conn = builder.Configuration.GetConnectionString("MySql");
builder.Services.AddDbContext<Comp584DbContext>(options =>
    options.UseMySql(
        conn,
        new MySqlServerVersion(new Version(8, 0, 36)) // match your MySQL server version
    ));

var app = builder.Build();

// HTTPS first 
app.UseHttpsRedirection();

// Use Swagger
app.UseSwagger();
app.UseSwaggerUI();

// CORS before MapControllers
app.UseCors("NgDev");

// Auth/Authorization (only if you’ve configured it)
app.UseAuthorization();

// Controllers
app.MapControllers();

// Simple health checks
app.MapGet("/", () => "Hello World!");
app.MapGet("/db-ping", async (Comp584DbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new { database = canConnect ? "up" : "down" });
});

app.Run();
