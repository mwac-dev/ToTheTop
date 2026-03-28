namespace LobbyServer.Models;

public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    public string Name { get; set; } = "";
    public bool IsReady { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class Lobby
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public List<Player> Players { get; set; } = new();
    public int MaxPlayers { get; set; } = 4;
    public string State { get; set; } = "waiting"; // waiting, ready, in_game
    public bool IsFull => Players.Count >= MaxPlayers;
    public bool AllReady => Players.Count > 1 && Players.All(p => p.IsReady);
}