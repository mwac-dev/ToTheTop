using Microsoft.AspNetCore.SignalR;
using ToTheTopDotnet.Hubs;
using ToTheTopDotnet.Models;

namespace ToTheTopDotnet.Services;

/// <summary>
/// Shared lobby cleanup logic used by both the SignalR hub (on disconnect)
/// and the controller (if needed).
/// </summary>
public static class LobbyCleanup
{
    // Reference to the same lobby store used by LobbyController.
    // In a real app this would be Redis or a DB — for now it's the static dictionary.
    public static Dictionary<string, Lobby> Lobbies { get; set; } = new();

    public static async Task RemovePlayer(
        string lobbyId,
        string playerId,
        IHubCallerClients clients,
        IGroupManager groups)
    {
        if (!Lobbies.TryGetValue(lobbyId, out var lobby))
            return;

        var player = lobby.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return;

        lobby.Players.Remove(player);
        Console.WriteLine($"[Lobby] Removed {player.Name} from {lobbyId}. {lobby.Players.Count} remaining.");

        if (lobby.Players.Count == 0)
        {
            // Empty lobby — destroy it
            Lobbies.Remove(lobbyId);
            Console.WriteLine($"[Lobby] Destroyed empty lobby {lobbyId}");
        }
        else
        {
            // Notify remaining players
            await clients.Group(lobbyId).SendAsync("PlayerLeft", new
            {
                playerId,
                lobby
            });
        }
    }
}
