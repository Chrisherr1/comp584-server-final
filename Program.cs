using Microsoft.EntityFrameworkCore;
using Comp584ServerFinal.Data.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.OpenApi.Models; 

var builder = WebApplication.CreateBuilder(args);

// Swagger Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT Bearer security definition so Swagger knows how to send tokens
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token.\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6..."
    });

    // Apply security requirement globally so all endpoints can use it
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Controllers
builder.Services.AddControllers()
.AddJsonOptions(options => {
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)) // now reading from appsettings.json
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
app.UseAuthentication();   // must come before authorization
// enables Authorization middleware
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
