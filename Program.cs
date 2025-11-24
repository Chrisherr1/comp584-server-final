using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions; // needed for UseMySQL
using Comp584ServerFinal.Data.Models;   // or .Data, depending on the file


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

// DbContext (Oracle provider)
var conn = builder.Configuration.GetConnectionString("MySql");
builder.Services.AddDbContext<Comp584DbContext>(options =>
    options.UseMySQL(conn)); // UseMySQL (Oracle provider)

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
