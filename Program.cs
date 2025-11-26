using Microsoft.EntityFrameworkCore;
using Comp584ServerFinal.Data.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Swagger Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Controllers
builder.Services.AddControllers();

// CORS setup (Angular dev server + Swagger browser calls)
builder.Services.AddCors(options =>
{
    options.AddPolicy("NgDev", policy =>
        policy.WithOrigins("http://localhost:4200")   // Angular dev server
            .AllowAnyHeader()
            .AllowAnyMethod());

    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// validates incoming JWTs by checking their issuer,audience and signiture 
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "yourapp",
            ValidAudience = "yourapp",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("SuperSecretKey12345"))
        };
    });

// DbContext (Pomelo provider)
var conn = builder.Configuration.GetConnectionString("MySql");
builder.Services.AddDbContext<Comp584DbContext>(options =>
    options.UseMySql(
        conn,
        new MySqlServerVersion(new Version(8, 0, 36)) // match your MySQL server version
    ));

var app = builder.Build();

// HTTPS redirection
app.UseHttpsRedirection();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Apply CORS (choose "NgDev" for Angular, or "AllowAll" for dev/testing)
app.UseCors("NgDev");

// enables Authentication middleware
app.UseAuthorization();
// enables Authroization middleware
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health checks
app.MapGet("/", () => "Hello World!");
app.MapGet("/db-ping", async (Comp584DbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new { database = canConnect ? "up" : "down" });
});

app.Run();
