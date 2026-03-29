using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class LobbyManager : MonoBehaviour
{
    [Header("Server Config")] [SerializeField]
    private string serverUrl = "http://localhost:5082";
    public string ServerUrl => serverUrl;

    [Header("Player Config")] [SerializeField]
    private string playerName = "UnityPlayer";

    //current state
    public string CurrentLobbyId { get; private set; }
    public string PlayerId { get; private set; }
    public LobbyData CurrentLobby { get; private set; }

    // Events
    public event Action<LobbyData> OnLobbyUpdated;
    public event Action<string> OnError;
    public event Action OnGameStarting;
    public event Action<string> OnConnectionStatusChanged;
     public event Action<string, string> OnGameEvent; // (eventType, jsonData)

    private HubConnection _hub;

    /// <summary>
    /// Join existing lobby by ID
    /// </summary>
    /// <param name="lobbyId"></param>
    public void JoinLobby(string lobbyId)
    {
        StartCoroutine(JoinLobbyCoroutine(lobbyId));
    }

    /// <summary>
    /// Toggle ready state
    /// </summary>
    /// <param name="isReady"></param>
    public void SetReady(bool isReady)
    {
        if (string.IsNullOrEmpty(CurrentLobbyId) || string.IsNullOrEmpty(PlayerId))
            return;
        StartCoroutine(SetReadyCoroutine(isReady));
    }

    /// <summary>
    /// Create a new lobby and join it.
    /// </summary>
    public void CreateAndJoin(string lobbyName)
    {
        StartCoroutine(CreateLobbyCoroutine(lobbyName));
    }

    // REST CALLS
    private IEnumerator CreateLobbyCoroutine(string lobbyName)
    {
        var json = JsonUtility.ToJson(new CreateRequest { name = lobbyName, maxPlayers = 4 });
        using var request = new UnityWebRequest($"{serverUrl}/api/lobby", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            OnError?.Invoke($"Create failed: {request.error}");
            yield break;
        }

        var response = JsonUtility.FromJson<CreateResponse>(request.downloadHandler.text);
        Debug.Log($"Created lobby: {response.id}");

        StartCoroutine(JoinLobbyCoroutine(response.id));
    }

    private IEnumerator JoinLobbyCoroutine(string lobbyId)
    {
        var json = JsonUtility.ToJson(new JoinRequest { playerName = playerName });
        using var request = new UnityWebRequest($"{serverUrl}/api/lobby/{lobbyId}/join", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            OnError?.Invoke($"Join failed: {request.error}");
            yield break;
        }

        var response = JsonUtility.FromJson<JoinResponse>(request.downloadHandler.text);
        CurrentLobbyId = lobbyId;
        PlayerId = response.playerId;
        CurrentLobby = response.lobby;

        Debug.Log($"Joined lobby: {CurrentLobbyId} as {PlayerId}");
        OnLobbyUpdated?.Invoke(CurrentLobby);

        // SignalR for real-time updates
        ConnectSignalR(lobbyId);
    }

    private IEnumerator SetReadyCoroutine(bool ready)
    {
        var json = JsonUtility.ToJson(new ReadyRequest { playerId = PlayerId, isReady = ready });
        using var request = new UnityWebRequest($"{serverUrl}/api/lobby/{CurrentLobbyId}/ready", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            OnError?.Invoke($"Ready failed: {request.error}");
        }
    }
    
    // Will be processed in Update
    private volatile LobbyData _pendingLobbyUpdate;
    private volatile bool _gameStarting;
    private volatile string _pendingStatus;
    private readonly ConcurrentQueue<(string type, string json)> _pendingGameEvents = new();

    // SignalR
    private async void ConnectSignalR(string lobbyId)
    {
        try
        {
            _hub = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}/hub/lobby")
                .WithAutomaticReconnect()
                .Build();
            // SignalR callbacks come in on a background thread
            // Unity APIs can only be called from main thread
            // will store data and then process in Update()


            // Handlers will march SendAsync calls in the C# backend
            _hub.On<LobbyEventData>("PlayerJoined", data =>
            {
                Debug.Log($"[SignalR] PlayerJoined: {data.player.name}");
                _pendingLobbyUpdate = data.lobby;
            });

            _hub.On<LobbyEventData>("PlayerReady", data =>
            {
                Debug.Log($"[SignalR] PlayerReady: {data.playerId} = {data.isReady}");
                _pendingLobbyUpdate = data.lobby;
            });

            _hub.On<LobbyEventData>("GameStarting", data =>
            {
                Debug.Log("[SignalR] Game starting!");
                _pendingLobbyUpdate = data.lobby;
                _gameStarting = true;
            });
            
             _hub.On<object>("GameCountdown", data =>
            {
                var json = data.ToString();
                Debug.Log($"[SignalR] GameCountdown: {json}");
                _pendingGameEvents.Enqueue(("GameCountdown", json));
            });
 
            _hub.On<object>("GamePlaying", data =>
            {
                var json = data.ToString();
                Debug.Log($"[SignalR] GamePlaying: {json}");
                _pendingGameEvents.Enqueue(("GamePlaying", json));
            });
 
            _hub.On<object>("GameTick", data =>
            {
                var json = data.ToString();
                _pendingGameEvents.Enqueue(("GameTick", json));
            });
 
            _hub.On<object>("GameOver", data =>
            {
                var json = data.ToString();
                Debug.Log($"[SignalR] GameOver: {json}");
                _pendingGameEvents.Enqueue(("GameOver", json));
            });

            _hub.Reconnecting += (ex) =>
            {
                Debug.Log("[SignalR] Reconnecting...");
                _pendingStatus = "reconnecting";
                return System.Threading.Tasks.Task.CompletedTask;
            };

            _hub.Reconnected += (connectionId) =>
            {
                Debug.Log("[SignalR] Reconnected");
                _pendingStatus = "connected";
                // Rejoin the lobby group after reconnection
                _ = _hub.InvokeAsync("JoinLobbyGroup", lobbyId);
                return System.Threading.Tasks.Task.CompletedTask;
            };

            _hub.Closed += (ex) =>
            {
                Debug.Log("[SignalR] Connection closed");
                _pendingStatus = "disconnected";
                return System.Threading.Tasks.Task.CompletedTask;
            };

            // Start the connection
            await _hub.StartAsync();
            OnConnectionStatusChanged?.Invoke("connected");
            Debug.Log("[SignalR] Connected");

            // Join the SignalR group for this lobby
            await _hub.InvokeAsync("JoinLobbyGroup", lobbyId);
            Debug.Log($"[SignalR] Joined group {lobbyId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SignalR] Connection failed: {ex.Message}");
            OnError?.Invoke($"SignalR connection failed: {ex.Message}");
            OnConnectionStatusChanged?.Invoke("error");
        }
    }

    // Main Thread
    // Will apply pending updates to the lobby data in Update()
    private void Update()
    {
        if (_pendingStatus != null)
        {
            var status = _pendingStatus;
            _pendingStatus = null;
            OnConnectionStatusChanged?.Invoke(status);
        }

        if (_pendingLobbyUpdate != null)
        {
            CurrentLobby = _pendingLobbyUpdate;
            _pendingLobbyUpdate = null;
            OnLobbyUpdated?.Invoke(CurrentLobby);
        }

        if (_gameStarting)
        {
            _gameStarting = false;
            OnGameStarting?.Invoke();
        }
        
        while (_pendingGameEvents.TryDequeue(out var gameEvent))
        {
            OnGameEvent?.Invoke(gameEvent.type, gameEvent.json);
        }
    }

    // CLEANUP
    private async void OnDestroy()
    {
        if (_hub != null)
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentLobbyId))
                    await _hub.InvokeAsync("LeaveLobbyGroup", CurrentLobbyId);
                await _hub.StopAsync();
                await _hub.DisposeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SignalR] Cleanup error: {ex.Message}");
            }
        }

        CurrentLobbyId = null;
    }

    // Serialization Types
    [Serializable]
    public class CreateRequest
    {
        public string name;
        public int maxPlayers;
    }

    [Serializable]
    public class JoinRequest
    {
        public string playerName;
    }

    [Serializable]
    public class ReadyRequest
    {
        public string playerId;
        public bool isReady;
    }

    [Serializable]
    public class CreateResponse
    {
        public string id;
        public string name;
    }

    [Serializable]
    public class JoinResponse
    {
        public string playerId;
        public LobbyData lobby;
    }
}

/// <summary>
/// lobby data model that matches server JSON schema
/// </summary>
[Serializable]
public class LobbyData
{
    public string id;
    public string name;
    public PlayerData[] players;
    public int maxPlayers;
    public string state;
}


[Serializable]
public class PlayerData
{
    public string id;
    public string name;
    public bool isReady;
}

/// <summary>
/// Event data from SignalR
/// Covers all event shapes
/// </summary>
[Serializable]
public class LobbyEventData
{
    public PlayerData player;
    public string playerId;
    public bool isReady;
    public LobbyData lobby;
}