using Microsoft.AspNetCore.SignalR;

namespace ToTheTopDotnet.Hubs;

public class LobbyHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[SignalR] Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    // clients call to join lobby's SignalR group
    // allows broadcasting to all in lobby X without tracking connections manually
    public async Task JoinLobbyGroup(string lobbyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        Console.WriteLine($"[SignalR] {Context.ConnectionId} joined group {lobbyId}");
    }

    public async Task LeaveLobbyGroup(string lobbyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[SignalR] Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}