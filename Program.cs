using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;

var builder = WebApplication.CreateBuilder(args);


//Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Ability to use Controllers instead of manual routing
builder.Services.AddControllers();

var app = builder.Build();

// Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

//Allows use UseAuthorization
app.UseAuthorization();
//Allows use Controllers
app.MapControllers();

//Get Route on Root
app.MapGet("/", () => "Hello World!");
//Runs the applciation
app.Run();
