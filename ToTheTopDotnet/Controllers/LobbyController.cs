using LobbyServer.Hubs;
using LobbyServer.Models;
using LobbyServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LobbyServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LobbyController : ControllerBase
{
    // for now in-memory, for projection would use Redis or DB 
    private static readonly Dictionary<string, Lobby> Lobbies = new();

    private readonly RabbitMqService _rabbit;
    private readonly IHubContext<LobbyHub> _hub;

    public LobbyController(RabbitMqService rabbit, IHubContext<LobbyHub> hub)
    {
        _rabbit = rabbit;
        _hub = hub;
    }


    /// GET /api/lobby - list all lobbies
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(Lobbies.Values);
    }

    ///Get /api/lobby/{id} - get lobby details
    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        return Lobbies.TryGetValue(id, out var lobby)
            ? Ok(lobby)
            : NotFound(new { error = "Lobby not found" });
    }

    /// POST /api/lobby - create a new lobby
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLobbyRequest request)
    {
        var lobby = new Lobby { Name = request.Name, MaxPlayers = request.MaxPlayers };
        Lobbies[lobby.Id] = lobby;

        await _rabbit.PublishAsync("lobby.created", new
        {
            lobbyId = lobby.Id,
            name = lobby.Name,
            timestamp = DateTime.UtcNow
        });

        return CreatedAtAction(nameof(Get), new { id = lobby.Id }, lobby);
    }

    /// POST /api/lobby/{id}/join - join a lobby
    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(string id, [FromBody] JoinRequest request)
    {
        if (!Lobbies.TryGetValue(id, out var lobby))
            return NotFound(new { error = "Lobby not found" });
        if (lobby.IsFull)
            return BadRequest(new { error = "Lobby is full" });
        var player = new Player { Name = request.PlayerName };
        lobby.Players.Add(player);


        await _rabbit.PublishAsync("lobby.player.joined", new
        {
            lobbyId = id,
            player = new { player.Id, player.Name },
            playerCount = lobby.Players.Count,
            timestamp = DateTime.UtcNow
        });

        await _hub.Clients.Group(id).SendAsync("PlayerJoined", new
        {
            player = new { player.Id, player.Name },
            lobby
        });

        return Ok(new { playerId = player.Id, lobby });
    }

    /// POST /api/lobby/{id}/ready - toggle ready state
    [HttpPost("{id}/ready")]
    public async Task<IActionResult> Ready(string id, [FromBody] ReadyRequest request)
    {
        if (!Lobbies.TryGetValue(id, out var lobby))
            return NotFound(new { error = "Lobby not found" });

        var player = lobby.Players.FirstOrDefault(p => p.Id == request.PlayerId);
        if (player == null)
            return NotFound(new { error = "Player not in lobby" });

        player.IsReady = request.IsReady;

        await _rabbit.PublishAsync("lobby.player.ready", new
        {
            lobbyId = id,
            playerId = player.Id,
            isReady = player.IsReady,
            timestamp = DateTime.UtcNow
        });

        await _hub.Clients.Group(id).SendAsync("PlayerReady",
            new
            {
                playerId = player.Id,
                isReady = player.IsReady,
                lobby
            });
        if (lobby.AllReady)
        {
            lobby.State = "in_game";

            await _rabbit.PublishAsync("lobby.game.starting", new
            {
                lobbyId = id,
                players = lobby.Players.Select(p => new { p.Id, p.Name}),
                timestamp = DateTime.UtcNow
            });

            await _hub.Clients.Group(id).SendAsync("GameStarting", new { lobby });
        }

        return Ok(new { lobby });
    }
}

public record CreateLobbyRequest(string Name, int MaxPlayers = 4);

public record JoinRequest(string PlayerName);

public record ReadyRequest(string PlayerId, bool IsReady);