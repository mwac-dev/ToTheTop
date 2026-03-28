using LobbyServer.Hubs;
using LobbyServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();


// rabbitmq as singleton
var rabbit = await RabbitMqService.CreateAsync();
builder.Services.AddSingleton(rabbit);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Vite dev
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // signalR
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<LobbyHub>("/hub/lobby"); // signalR endpoint

Console.WriteLine("Lobby server running on http://localhost:5082");
app.Run("http://localhost:5082");