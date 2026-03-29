using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the full UI lifecycle: join → lobby → countdown → game → results.
/// Listens to LobbyManager for both lobby and game events.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyManager lobbyManager;

    [Header("Panels")]
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject resultsPanel;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private TMP_InputField lobbyIdInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;

    [Header("Lobby Panel")]
    [SerializeField] private TextMeshProUGUI lobbyTitle;
    [SerializeField] private TextMeshProUGUI lobbyStatus;
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerCardPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;

    [Header("Countdown Panel")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Game Panel")]
    [SerializeField] private Transform barsContainer;
    [SerializeField] private GameObject playerBarPrefab;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button tapButton;

    [Header("Results Panel")]
    [SerializeField] private TextMeshProUGUI resultsText;

    [Header("Game Settings")]
    [SerializeField] private float lerpSpeed = 10f;

    // Lobby state
    private bool isReady;

    // Game state
    private bool _gameActive;
    private readonly Dictionary<string, PlayerBarUI> _bars = new();
    private readonly Dictionary<string, float> _targetValues = new();

    private void Start()
    {
        // Show only join panel initially
        ShowPanel(joinPanel);

        // Join panel buttons
        createButton.onClick.AddListener(() =>
        {
            var name = lobbyNameInput.text.Trim();
            if (!string.IsNullOrEmpty(name))
                lobbyManager.CreateAndJoin(name);
        });

        joinButton.onClick.AddListener(() =>
        {
            var id = lobbyIdInput.text.Trim();
            if (!string.IsNullOrEmpty(id))
                lobbyManager.JoinLobby(id);
        });

        // Lobby panel buttons
        readyButton.onClick.AddListener(() =>
        {
            isReady = !isReady;
            lobbyManager.SetReady(isReady);
            readyButtonText.text = isReady ? "READY!" : "Ready Up";
        });

        // Game panel buttons
        tapButton.onClick.AddListener(SendTap);

        // Subscribe to lobby events
        lobbyManager.OnLobbyUpdated += UpdateLobbyDisplay;
        lobbyManager.OnGameStarting += HandleGameStarting;
        lobbyManager.OnError += (err) => Debug.LogError(err);

        // Subscribe to game events
        lobbyManager.OnGameEvent += HandleGameEvent;
    }

    private void Update()
    {
        // Spacebar tap input
        if (_gameActive && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SendTap();
        }

        // Smoothly lerp all bars toward their server target values
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

    private void ShowPanel(GameObject panel)
    {
        joinPanel.SetActive(panel == joinPanel);
        lobbyPanel.SetActive(panel == lobbyPanel);
        countdownPanel.SetActive(panel == countdownPanel);
        gamePanel.SetActive(panel == gamePanel);
        resultsPanel.SetActive(panel == resultsPanel);
    }

    // =====================
    // Lobby Events
    // =====================

    private void UpdateLobbyDisplay(LobbyData lobby)
    {
        ShowPanel(lobbyPanel);

        lobbyTitle.text = lobby.name;
        lobbyStatus.text = $"{lobby.players.Length}/{lobby.maxPlayers} players — {lobby.state}";

        // Clear and rebuild player cards
        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        foreach (var player in lobby.players)
        {
            var card = Instantiate(playerCardPrefab, playerListParent);
            var nameText = card.transform.Find("Name").GetComponent<TextMeshProUGUI>();
            var statusText = card.transform.Find("Status").GetComponent<TextMeshProUGUI>();

            nameText.text = player.name;
            statusText.text = player.isReady ? "READY" : "Waiting...";
            statusText.color = player.isReady ? Color.green : Color.gray;
        }
    }

    private void HandleGameStarting()
    {
        lobbyStatus.text = "GAME STARTING!";
        readyButton.interactable = false;
        // Countdown panel will take over once GameCountdown events arrive
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

        ShowPanel(countdownPanel);

        int count = Mathf.CeilToInt((float)data.countdown);
        countdownText.text = count > 0 ? count.ToString() : "GO!";
    }

    private void HandleGamePlaying(string json)
    {
        ShowPanel(gamePanel);
        _gameActive = true;
        CreatePlayerBars();
    }

    private void HandleTick(string json)
    {
        var data = JsonUtility.FromJson<TickData>(json);

        timerText.text = $"{Mathf.CeilToInt(data.timeRemaining)}s";

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
        ShowPanel(resultsPanel);

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

        resultsText.text = sb.ToString();
    }

    // =====================
    // Game Bar Creation
    // =====================

    private void CreatePlayerBars()
    {
        foreach (Transform child in barsContainer)
            Destroy(child.gameObject);
        _bars.Clear();
        _targetValues.Clear();

        if (lobbyManager.CurrentLobby?.players == null) return;

        foreach (var player in lobbyManager.CurrentLobby.players)
        {
            var barObj = Instantiate(playerBarPrefab, barsContainer);
            var barUI = barObj.GetComponent<PlayerBarUI>();

            if (barUI != null)
            {
                bool isLocal = player.id == lobbyManager.PlayerId;
                barUI.Setup(player.name, player.id, isLocal);
                _bars[player.id] = barUI;
                _targetValues[player.id] = 0f;
            }
        }
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
}
