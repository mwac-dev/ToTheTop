using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using ToTheTopDotnet.Services;

namespace ToTheTopDotnet.Hubs;

public class LobbyHub : Hub
{
    // Track connection → (lobbyId, playerId)
    private static readonly ConcurrentDictionary<string, (string lobbyId, string playerId)> Connections = new();

    public async Task JoinLobbyGroup(string lobbyId, string? playerId = null)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

        if (playerId != null)
            Connections[Context.ConnectionId] = (lobbyId, playerId);

        Console.WriteLine($"[SignalR] {Context.ConnectionId} joined group {lobbyId}");
    }

    public async Task LeaveLobbyGroup(string lobbyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        Connections.TryRemove(Context.ConnectionId, out _);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Connections.TryRemove(Context.ConnectionId, out var info))
        {
            Console.WriteLine($"[SignalR] {info.playerId} disconnected from lobby {info.lobbyId}");

            await LobbyCleanup.RemovePlayer(info.lobbyId, info.playerId, Clients, Groups);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
