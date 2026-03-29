using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the full UI lifecycle: join → lobby → countdown → game → results.
/// Listens to LobbyManager for both lobby and game events.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyManager lobbyManager;

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset lobbyListItemTemplate;
    [SerializeField] private VisualTreeAsset playerCardTemplate;
    [SerializeField] private VisualTreeAsset playerBarTemplate;

    [Header("Game Settings")]
    [SerializeField] private float lerpSpeed = 10f;

    // Panels
    private VisualElement _joinPanel;
    private VisualElement _lobbyPanel;
    private VisualElement _countdownPanel;
    private VisualElement _gamePanel;
    private VisualElement _resultsPanel;
    private VisualElement[] _allPanels;

    // Join panel elements
    private TextField _lobbyNameInput;
    private Button _createButton;
    private ScrollView _lobbyListScroll;
    private Button _refreshButton;
    private Label _lobbyListStatus;

    // Lobby panel elements
    private Label _lobbyTitle;
    private Label _lobbyStatus;
    private VisualElement _playerList;
    private Button _readyButton;
    private Button _leaveButton;

    // Countdown panel elements
    private Label _countdownText;

    // Game panel elements
    private VisualElement _barsContainer;
    private Label _timerText;
    private Button _tapButton;

    // Results panel elements
    private Label _resultsText;
    private Button _playAgainButton;
    private Button _quitButton;

    // Lobby state
    private bool isReady;

    // Game state
    private bool _gameActive;
    private readonly Dictionary<string, PlayerBarUI> _bars = new();
    private readonly Dictionary<string, float> _targetValues = new();

    private void Start()
    {
        var root = uiDocument.rootVisualElement;

        // Query panels
        _joinPanel = root.Q("join-panel");
        _lobbyPanel = root.Q("lobby-panel");
        _countdownPanel = root.Q("countdown-panel");
        _gamePanel = root.Q("game-panel");
        _resultsPanel = root.Q("results-panel");
        _allPanels = new[] { _joinPanel, _lobbyPanel, _countdownPanel, _gamePanel, _resultsPanel };

        // Query join panel
        _lobbyNameInput = root.Q<TextField>("lobby-name-input");
        _createButton = root.Q<Button>("create-button");
        _lobbyListScroll = root.Q<ScrollView>("lobby-list-scroll");
        _refreshButton = root.Q<Button>("refresh-button");
        _lobbyListStatus = root.Q<Label>("lobby-list-status");

        // Query lobby panel
        _lobbyTitle = root.Q<Label>("lobby-title");
        _lobbyStatus = root.Q<Label>("lobby-status");
        _playerList = root.Q("player-list");
        _readyButton = root.Q<Button>("ready-button");
        _leaveButton = root.Q<Button>("leave-button");

        // Query countdown panel
        _countdownText = root.Q<Label>("countdown-text");

        // Query game panel
        _barsContainer = root.Q("bars-container");
        _timerText = root.Q<Label>("timer-text");
        _tapButton = root.Q<Button>("tap-button");

        // Query results panel
        _resultsText = root.Q<Label>("results-text");
        _playAgainButton = root.Q<Button>("play-again-button");
        _quitButton = root.Q<Button>("quit-button");

        ShowPanel(_joinPanel);

        // Create lobby
        _createButton.clicked += () =>
        {
            var name = _lobbyNameInput.value.Trim();
            if (!string.IsNullOrEmpty(name))
                lobbyManager.CreateAndJoin(name);
        };

        // Refresh lobby list
        _refreshButton.clicked += () => StartCoroutine(FetchLobbies());

        // Lobby panel
        _readyButton.clicked += () =>
        {
            isReady = !isReady;
            lobbyManager.SetReady(isReady);
            _readyButton.text = isReady ? "READY!" : "Ready Up";
        };

        // Leave lobby
        _leaveButton.clicked += LeaveLobby;

        // Results panel
        _playAgainButton.clicked += LeaveLobby;
        _quitButton.clicked += () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        };

        // Game panel
        _tapButton.clicked += SendTap;

        // Subscribe to events
        lobbyManager.OnLobbyUpdated += UpdateLobbyDisplay;
        lobbyManager.OnGameStarting += HandleGameStarting;
        lobbyManager.OnError += (err) => Debug.LogError(err);
        lobbyManager.OnGameEvent += HandleGameEvent;

        // Fetch lobbies on start
        StartCoroutine(FetchLobbies());
    }

    private void Update()
    {
        if (_gameActive && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SendTap();
        }

        foreach (var (playerId, bar) in _bars)
        {
            if (_targetValues.TryGetValue(playerId, out var target))
            {
                bar.CurrentValue = Mathf.Lerp(bar.CurrentValue, target, Time.deltaTime * lerpSpeed);
                bar.UpdateFill(bar.CurrentValue / 100f);
            }
        }
    }

    // =====================
    // Panel Management
    // =====================

    private void ShowPanel(VisualElement panel)
    {
        foreach (var p in _allPanels)
            p.style.display = p == panel ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // =====================
    // Lobby List
    // =====================

    private IEnumerator FetchLobbies()
    {
        _lobbyListStatus.text = "Loading...";

        using var request = UnityWebRequest.Get($"{lobbyManager.ServerUrl}/api/lobby");
        yield return request.SendWebRequest();

        _lobbyListScroll.Clear();

        if (request.result != UnityWebRequest.Result.Success)
        {
            _lobbyListStatus.text = "Failed to load lobbies";
            yield break;
        }

        // JsonUtility can't deserialize top-level arrays
        var json = "{\"items\":" + request.downloadHandler.text + "}";
        var lobbyList = JsonUtility.FromJson<LobbyListWrapper>(json);

        if (lobbyList.items == null || lobbyList.items.Length == 0)
        {
            _lobbyListStatus.text = "No lobbies — create one!";
            yield break;
        }

        _lobbyListStatus.text = "";

        foreach (var lobby in lobbyList.items)
        {
            if (lobby.state == "in_game") continue;

            var item = lobbyListItemTemplate.Instantiate();

            var nameText = item.Q<Label>("Name");
            var infoText = item.Q<Label>("Info");
            var joinBtn = item.Q<Button>("JoinButton");

            nameText.text = lobby.name;
            infoText.text = $"{lobby.players.Length}/{lobby.maxPlayers} players";

            bool isFull = lobby.players.Length >= lobby.maxPlayers;
            joinBtn.SetEnabled(!isFull);

            var lobbyId = lobby.id;
            joinBtn.clicked += () => lobbyManager.JoinLobby(lobbyId);

            _lobbyListScroll.Add(item);
        }
    }

    // =====================
    // Lobby Events
    // =====================

    private void UpdateLobbyDisplay(LobbyData lobby)
    {
        ShowPanel(_lobbyPanel);

        _lobbyTitle.text = lobby.name;
        _lobbyStatus.text = $"{lobby.players.Length}/{lobby.maxPlayers} players — {lobby.state}";

        _playerList.Clear();

        foreach (var player in lobby.players)
        {
            var card = playerCardTemplate.Instantiate();
            var nameText = card.Q<Label>("Name");
            var statusText = card.Q<Label>("Status");

            nameText.text = player.name;
            statusText.text = player.isReady ? "READY" : "Waiting...";

            statusText.RemoveFromClassList("player-card__status--ready");
            if (player.isReady)
                statusText.AddToClassList("player-card__status--ready");

            _playerList.Add(card);
        }
    }

    private void HandleGameStarting()
    {
        _lobbyStatus.text = "GAME STARTING!";
        _readyButton.SetEnabled(false);
    }

    // =====================
    // Game Events
    // =====================

    private void HandleGameEvent(string eventType, string json)
    {
        switch (eventType)
        {
            case "GameCountdown":
                HandleCountdown(json);
                break;
            case "GamePlaying":
                HandleGamePlaying(json);
                break;
            case "GameTick":
                HandleTick(json);
                break;
            case "GameOver":
                HandleGameOver(json);
                break;
        }
    }

    private void HandleCountdown(string json)
    {
        var data = JsonUtility.FromJson<CountdownData>(json);
        ShowPanel(_countdownPanel);
        int count = Mathf.CeilToInt((float)data.countdown);
        _countdownText.text = count > 0 ? count.ToString() : "GO!";
    }

    private void HandleGamePlaying(string json)
    {
        ShowPanel(_gamePanel);
        _gameActive = true;
        CreatePlayerBars();
    }

    private void HandleTick(string json)
    {
        var data = JsonUtility.FromJson<TickData>(json);
        _timerText.text = $"{Mathf.CeilToInt(data.timeRemaining)}s";

        foreach (var player in data.players)
        {
            _targetValues[player.id] = player.value;

            if (_bars.TryGetValue(player.id, out var bar))
            {
                bar.SetTapCount(player.tapCount);
                bar.SetPlatform(player.platform);
            }
        }
    }

    private void HandleGameOver(string json)
    {
        _gameActive = false;
        ShowPanel(_resultsPanel);

        var data = JsonUtility.FromJson<GameOverData>(json);

        var sb = new StringBuilder();
        sb.AppendLine("<size=32>GAME OVER</size>\n");
        sb.AppendLine($"Reason: {data.reason}\n");

        foreach (var result in data.results)
        {
            string crown = result.id == data.winnerId ? " <color=#FFD700>WINNER</color>" : "";
            string platform = result.platform == "browser" ? "[WEB]" : "[PC]";
            sb.AppendLine($"#{result.rank}  {platform} {result.name} — {result.tapCount} taps{crown}");
        }

        _resultsText.text = sb.ToString();
    }

    // =====================
    // Game Bar Creation
    // =====================

    private void CreatePlayerBars()
    {
        _barsContainer.Clear();
        _bars.Clear();
        _targetValues.Clear();

        if (lobbyManager.CurrentLobby?.players == null) return;

        foreach (var player in lobbyManager.CurrentLobby.players)
        {
            var barElement = playerBarTemplate.Instantiate();
            bool isLocal = player.id == lobbyManager.PlayerId;
            var barUI = new PlayerBarUI(barElement, player.name, player.id, isLocal);

            _barsContainer.Add(barElement);
            _bars[player.id] = barUI;
            _targetValues[player.id] = 0f;
        }
    }

    // =====================
    // Leave Lobby
    // =====================

    private void LeaveLobby()
    {
        lobbyManager.LeaveLobby();
        _gameActive = false;
        _bars.Clear();
        _targetValues.Clear();
        isReady = false;
        _readyButton.text = "Ready Up";
        _readyButton.SetEnabled(true);
        ShowPanel(_joinPanel);
        StartCoroutine(FetchLobbies());
    }

    // =====================
    // Tap Input
    // =====================

    private void SendTap()
    {
        if (!_gameActive) return;
        if (string.IsNullOrEmpty(lobbyManager.CurrentLobbyId)) return;
        StartCoroutine(SendTapCoroutine());
    }

    private IEnumerator SendTapCoroutine()
    {
        var json = JsonUtility.ToJson(new TapRequest { playerId = lobbyManager.PlayerId });
        using var request = new UnityWebRequest(
            $"{lobbyManager.ServerUrl}/api/game/{lobbyManager.CurrentLobbyId}/tap", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
    }

    // =====================
    // Cleanup
    // =====================

    private void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyUpdated -= UpdateLobbyDisplay;
            lobbyManager.OnGameStarting -= HandleGameStarting;
            lobbyManager.OnGameEvent -= HandleGameEvent;
        }
    }

    // =====================
    // Serialization Types
    // =====================

    [Serializable] public class TapRequest { public string playerId; }
    [Serializable] public class CountdownData { public double countdown; }

    [Serializable]
    public class TickData
    {
        public TickPlayer[] players;
        public float timeRemaining;
    }

    [Serializable]
    public class TickPlayer
    {
        public string id;
        public string name;
        public string platform;
        public float value;
        public int tapCount;
    }

    [Serializable]
    public class GameOverData
    {
        public string winnerId;
        public string reason;
        public GameOverResult[] results;
    }

    [Serializable]
    public class GameOverResult
    {
        public string id;
        public string name;
        public string platform;
        public float value;
        public int tapCount;
        public int rank;
    }

    [Serializable]
    public class LobbyListItem
    {
        public string id;
        public string name;
        public PlayerData[] players;
        public int maxPlayers;
        public string state;
    }

    [Serializable]
    public class LobbyListWrapper
    {
        public LobbyListItem[] items;
    }
}
