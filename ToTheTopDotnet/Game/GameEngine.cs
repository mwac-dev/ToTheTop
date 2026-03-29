using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using ToTheTopDotnet.Hubs;
using ToTheTopDotnet.Services;

namespace ToTheTopDotnet.Game;

public class PlayerGameState
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Platform { get; set; } = "desktop"; // desktop or browser
    public float Value { get; set; } = 0f; // 0 to 100 for the bar
    public int TapCount { get; set; } = 0;
}

public class GameInstance
{
    public string LobbyId { get; set; } = "";
    public Dictionary<string, PlayerGameState> Players { get; set; } = new();
    public string State { get; set; } = "countdown"; // countdown, playing, finished
    public float CountdownRemaining { get; set; } = 3f;
    public float TimeRemaining { get; set; } = 15f;
    public string? WinnerId { get; set; }
}

/// <summary>
/// Manages all active game instances
/// Runs fixed-rate tick loop that updates game state and broadcasts updates to clients
/// </summary>
public class GameEngine : IHostedService, IDisposable
{
    private readonly IHubContext<LobbyHub> _hub;
    private readonly RabbitMqService _rabbit;
    private readonly ConcurrentDictionary<string, GameInstance> _games = new();
    private Timer? _tickTimer;

    private const float TickRate = 20f; // ticks per second
    private const float TapIncrement = 3.5f; // how much one tap adds
    private const float DecayPerSecond = 8f; // how fast bar falls
    private const float WinThreshold = 100f; // first to this value wins game
    private const float GameDuration = 15f; //second
    private const float CountdownDuration = 3f; //seconds before game starts


    public GameEngine(IHubContext<LobbyHub> hub, RabbitMqService rabbit)
    {
        _hub = hub;
        _rabbit = rabbit;
    }

    /// <summary>
    /// Start a new game for a lobby. Called when all players are ready.
    /// </summary>
    public void StartGame(string lobbyId, IEnumerable<(string id, string name, string platform)> players)
    {
        var game = new GameInstance
        {
            LobbyId = lobbyId,
            State = "countdown",
            CountdownRemaining = CountdownDuration,
            TimeRemaining = GameDuration
        };

        foreach (var (id, name, platform) in players)
        {
            game.Players[id] = new PlayerGameState
            {
                Id = id,
                Name = name,
                Platform = platform,
                Value = 0f
            };
        }

        _games[lobbyId] = game;
        Console.WriteLine($"[GameEngine] Game started for lobby {lobbyId} with {game.Players.Count} players");

        _ = _rabbit.PublishAsync("game.started", new
        {
            lobbyId,
            players = game.Players.Values.Select(p => new { p.Id, p.Name, p.Platform }),
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle a tap from a player. Just increments their value.
    /// The tick loop handles broadcasting.
    /// </summary>
    public void HandleTap(string lobbyId, string playerId)
    {
        if (!_games.TryGetValue(lobbyId, out var game))
            return;

        if (game.State != "playing")
            return;

        if (!game.Players.TryGetValue(playerId, out var player))
            return;

        player.Value = Math.Min(WinThreshold, player.Value + TapIncrement);
        player.TapCount++;
    }

    /// <summary>
    /// Main game loop. Runs at TickRate per second
    /// Updates active games and broadcasts state
    /// </summary>
    /// <param name="state"></param>
    private void Tick(object? state)
    {
        var dt = 1f / TickRate;
        foreach (var (lobbyId, game) in _games)
        {
            switch (game.State)
            {
                case "countdown":
                    TickCountdown(lobbyId, game, dt);
                    break;
                case "playing":
                    TickPlaying(lobbyId, game, dt);
                    break;
                case "finished":
                    break;
            }
        }
    }

    private void TickCountdown(string lobbyId, GameInstance game, float dt)
    {
        game.CountdownRemaining -= dt;

        _ = _hub.Clients.Group(lobbyId).SendAsync("GameCountdown", new
        {
            countdown = Math.Ceiling(game.CountdownRemaining),
        });

        if (game.CountdownRemaining <= 0)
        {
            game.State = "playing";
            _ = _hub.Clients.Group(lobbyId).SendAsync("GamePlaying", new
            {
                duration = GameDuration,
            });

            _ = _rabbit.PublishAsync("game.playing", new
            {
                lobbyId,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private void TickPlaying(string lobbyId, GameInstance game, float dt)
    {
        game.TimeRemaining -= dt;

        // Apply decay to players
        foreach (var player in game.Players.Values)
        {
            player.Value = Math.Max(0f, player.Value - DecayPerSecond * dt);
        }

        var winner = game.Players.Values.FirstOrDefault(p => p.Value >= WinThreshold);
        if (winner != null)
        {
            EndGame(lobbyId, game, winner.Id, "reached_top");
            return;
        }

        // check for timeout
        if (game.TimeRemaining <= 0)
        {
            var highest = game.Players.Values.OrderByDescending(p => p.Value).First();
            EndGame(lobbyId, game, highest.Id, "timeout");
            return;
        }

        // Broadcast to all clients
        _ = _hub.Clients.Group(lobbyId).SendAsync("GameTick", new
        {
            players = game.Players.Values.Select(p => new
            {
                p.Id,
                p.Name,
                p.Platform,
                p.Value,
                p.TapCount
            }),
            timeRemaining = game.TimeRemaining
        });
    }

    private void EndGame(string lobbyId, GameInstance game, string winnerId, string reason)
    {
        game.State = "finished";
        game.WinnerId = winnerId;

        var results = game.Players.Values
            .OrderByDescending(p => p.Value)
            .Select((p, i) => new
            {
                p.Id,
                p.Name,
                p.Platform,
                p.Value,
                p.TapCount,
                rang = i + 1
            })
            .ToList();

        _ = _hub.Clients.Group(lobbyId).SendAsync("GameOver", new
        {
            winnerId,
            reason,
            results
        });

        _ = _rabbit.PublishAsync("game.over", new
        {
            lobbyId,
            winnerId,
            reason,
            results,
            timestamp = DateTime.UtcNow
        });
        Console.WriteLine($"[GameEngine] Game over in {lobbyId}. Winer: {winnerId} ({reason})");

        // cleanup after delay so clients can show results screen
        _ = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
        {
            _games.TryRemove(lobbyId, out GameInstance _);
            Console.WriteLine($"[GameEngine] Cleaned up game {lobbyId}");
        });
    }

    /// <summary>
    /// Check if game is active for given lobby
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <returns></returns>
    public bool HasActiveGame(string lobbyId) => _games.ContainsKey(lobbyId);

    /// <summary>
    /// Get game instance for given lobby
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <returns></returns>
    public GameInstance? GetGame(string lobbyId) => _games.GetValueOrDefault(lobbyId);


    public Task StartAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(1.0 / TickRate);
        _tickTimer = new Timer(Tick, null, interval, interval);
        Console.WriteLine($"[GameEngine] Started at {TickRate} ticks/sec");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _tickTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _tickTimer?.Dispose();
    }
}