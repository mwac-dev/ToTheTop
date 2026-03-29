using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

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

    private VisualElement _joinPanel;
    private VisualElement _lobbyPanel;
    private VisualElement _countdownPanel;
    private VisualElement _gamePanel;
    private VisualElement _resultsPanel;
    private VisualElement[] _allPanels;

    private TextField _lobbyNameInput;
    private Button _createButton;
    private ScrollView _lobbyListScroll;
    private Button _refreshButton;
    private Label _lobbyListStatus;

    private Label _lobbyTitle;
    private Label _lobbyStatus;
    private VisualElement _playerList;
    private Button _readyButton;
    private Button _leaveButton;

    private Label _countdownText;
    private VisualElement _countdownFlash;

    private VisualElement _barsContainer;
    private Label _timerText;
    private Button _tapButton;

    private Label _resultsTitle;
    private Label _resultsReason;
    private VisualElement _resultsList;
    private Button _playAgainButton;
    private Button _quitButton;

    private bool isReady;
    private bool _gameActive;
    private readonly Dictionary<string, PlayerBarUI> _bars = new();
    private readonly Dictionary<string, float> _targetValues = new();

    private void Start()
    {
        var root = uiDocument.rootVisualElement;

        _joinPanel = root.Q("join-panel");
        _lobbyPanel = root.Q("lobby-panel");
        _countdownPanel = root.Q("countdown-panel");
        _gamePanel = root.Q("game-panel");
        _resultsPanel = root.Q("results-panel");
        _allPanels = new[] { _joinPanel, _lobbyPanel, _countdownPanel, _gamePanel, _resultsPanel };

        _lobbyNameInput = root.Q<TextField>("lobby-name-input");
        _createButton = root.Q<Button>("create-button");
        _lobbyListScroll = root.Q<ScrollView>("lobby-list-scroll");
        _refreshButton = root.Q<Button>("refresh-button");
        _lobbyListStatus = root.Q<Label>("lobby-list-status");

        _lobbyTitle = root.Q<Label>("lobby-title");
        _lobbyStatus = root.Q<Label>("lobby-status");
        _playerList = root.Q("player-list");
        _readyButton = root.Q<Button>("ready-button");
        _leaveButton = root.Q<Button>("leave-button");

        _countdownText = root.Q<Label>("countdown-text");
        _countdownFlash = root.Q("countdown-flash");

        _barsContainer = root.Q("bars-container");
        _timerText = root.Q<Label>("timer-text");
        _tapButton = root.Q<Button>("tap-button");

        _resultsTitle = root.Q<Label>("results-title");
        _resultsReason = root.Q<Label>("results-reason");
        _resultsList = root.Q("results-list");
        _playAgainButton = root.Q<Button>("play-again-button");
        _quitButton = root.Q<Button>("quit-button");

        ShowPanel(_joinPanel);

        _createButton.clicked += () =>
        {
            var name = _lobbyNameInput.value.Trim();
            if (!string.IsNullOrEmpty(name))
                lobbyManager.CreateAndJoin(name);
        };

        _refreshButton.clicked += () => StartCoroutine(FetchLobbies());

        _readyButton.clicked += () =>
        {
            isReady = !isReady;
            lobbyManager.SetReady(isReady);
            _readyButton.text = isReady ? "READY!" : "READY UP";
            StartCoroutine(BounceElement(_readyButton, 0.2f));
        };

        _leaveButton.clicked += LeaveLobby;

        _playAgainButton.clicked += () =>
        {
            StartCoroutine(BounceElement(_playAgainButton, 0.2f));
            ReturnToLobby();
        };
        _quitButton.clicked += () =>
        {
            LeaveLobby();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        };

        _tapButton.clicked += () =>
        {
            SendTap();
            StartCoroutine(BounceElement(_tapButton, 0.2f));
        };

        lobbyManager.OnLobbyUpdated += UpdateLobbyDisplay;
        lobbyManager.OnGameStarting += HandleGameStarting;
        lobbyManager.OnError += (err) => Debug.LogError(err);
        lobbyManager.OnGameEvent += HandleGameEvent;

        StartCoroutine(FetchLobbies());
    }

    private void Update()
    {
        if (_gameActive && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SendTap();
            StartCoroutine(BounceElement(_tapButton, 0.2f));
        }

        foreach (var (playerId, bar) in _bars)
        {
            if (_targetValues.TryGetValue(playerId, out var target))
            {
                bar.CurrentValue = Mathf.Lerp(bar.CurrentValue, target, Time.deltaTime * lerpSpeed);
                bar.UpdateFill(bar.CurrentValue / 100f);
                bar.ApplyShake(bar.CurrentValue);
                bar.AnimateStripes(Time.deltaTime, bar.CurrentValue);
            }
        }
    }

    private IEnumerator BounceElement(VisualElement el, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float squash = 1f + 0.15f * Mathf.Sin(progress * Mathf.PI * 2.5f) * (1f - progress);
            float stretch = 2f - squash;
            el.style.scale = new Scale(new Vector2(stretch, squash));
            yield return null;
        }
        el.style.scale = new Scale(Vector2.one);
    }

    private IEnumerator CountdownPop(Label label, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float scale;
            if (progress < 0.4f)
                scale = Mathf.Lerp(2.5f, 0.9f, progress / 0.4f);
            else if (progress < 0.6f)
                scale = Mathf.Lerp(0.9f, 1.08f, (progress - 0.4f) / 0.2f);
            else
                scale = Mathf.Lerp(1.08f, 1f, (progress - 0.6f) / 0.4f);

            label.style.scale = new Scale(Vector2.one * scale);
            yield return null;
        }
        label.style.scale = new Scale(Vector2.one);
    }

    private IEnumerator FlashScreen(VisualElement flashEl, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0.35f, 0f, t / duration);
            flashEl.style.backgroundColor = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        flashEl.style.backgroundColor = new Color(1f, 1f, 1f, 0f);
    }

    private void ShowPanel(VisualElement panel)
    {
        foreach (var p in _allPanels)
            p.style.display = p == panel ? DisplayStyle.Flex : DisplayStyle.None;
    }

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

    private void UpdateLobbyDisplay(LobbyData lobby)
    {
        if (_resultsPanel.style.display == DisplayStyle.Flex)
        {
            _lobbyTitle.text = lobby.name;
            _lobbyStatus.text = $"{lobby.players.Length}/{lobby.maxPlayers} players — {lobby.state}";
            return;
        }

        ShowPanel(_lobbyPanel);

        _lobbyTitle.text = lobby.name;
        _lobbyStatus.text = $"{lobby.players.Length}/{lobby.maxPlayers} players — {lobby.state}";

        _playerList.Clear();

        foreach (var player in lobby.players)
        {
            var card = playerCardTemplate.Instantiate();
            var nameText = card.Q<Label>("Name");
            var statusText = card.Q<Label>("Status");
            var cardEl = card.Q(className: "player-card");

            nameText.text = player.name;
            statusText.text = player.isReady ? "READY" : "Waiting...";

            statusText.RemoveFromClassList("player-card__status--ready");
            if (cardEl != null)
                cardEl.RemoveFromClassList("player-card--ready");

            if (player.isReady)
            {
                statusText.AddToClassList("player-card__status--ready");
                if (cardEl != null)
                    cardEl.AddToClassList("player-card--ready");
            }

            _playerList.Add(card);
        }
    }

    private void HandleGameStarting()
    {
        _lobbyStatus.text = "GAME STARTING!";
        _readyButton.SetEnabled(false);
    }

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

        StopCoroutine(nameof(CountdownPop));
        StartCoroutine(CountdownPop(_countdownText, 0.5f));

        if (count <= 0)
        {
            _countdownText.style.color = new Color(1f, 0.42f, 0.17f);
            if (_countdownFlash != null)
                StartCoroutine(FlashScreen(_countdownFlash, 0.4f));
        }
        else
        {
            _countdownText.style.color = new Color(0.94f, 0.95f, 1f);
        }
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
        int timeLeft = Mathf.CeilToInt(data.timeRemaining);
        _timerText.text = $"{timeLeft}s";

        if (data.timeRemaining <= 5f)
        {
            _timerText.style.color = new Color(1f, 0.28f, 0.34f);
            if (!_timerText.ClassListContains("timer--urgent"))
                _timerText.AddToClassList("timer--urgent");
            float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 12f);
            _timerText.style.scale = new Scale(Vector2.one * pulse);
        }
        else if (data.timeRemaining <= 10f)
        {
            _timerText.style.color = new Color(1f, 0.28f, 0.34f);
            _timerText.style.scale = new Scale(Vector2.one);
        }
        else
        {
            _timerText.style.color = new Color(0.94f, 0.95f, 1f);
            _timerText.RemoveFromClassList("timer--urgent");
            _timerText.style.scale = new Scale(Vector2.one);
        }

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

        if (_resultsTitle != null)
            _resultsTitle.text = "GAME OVER";
        if (_resultsReason != null)
            _resultsReason.text = data.reason == "reached_top" ? "Someone reached the top!" : "Time ran out!";

        if (_resultsList != null)
        {
            _resultsList.Clear();

            for (int i = 0; i < data.results.Length; i++)
            {
                var result = data.results[i];
                bool isWinner = result.id == data.winnerId;
                bool isLocal = result.id == lobbyManager.PlayerId;
                string platform = result.platform == "browser" ? "\U0001F310" : "\U0001F5A5\uFE0F";
                string crown = isWinner ? "\U0001F451 " : "";

                var row = new VisualElement();
                row.AddToClassList("result-row");
                if (isWinner) row.AddToClassList("result-row--winner");
                if (isLocal) row.AddToClassList("result-row--local");

                row.style.opacity = 0f;
                row.style.translate = new Translate(40f, 0f);

                var rankLabel = new Label($"{crown}#{result.rank}");
                rankLabel.AddToClassList("result-row__rank");
                if (isWinner) rankLabel.AddToClassList("result-row__rank--winner");

                var nameLabel = new Label(result.name);
                nameLabel.AddToClassList("result-row__name");

                var platformLabel = new Label(platform);
                platformLabel.AddToClassList("result-row__platform");

                var valueLabel = new Label($"{Mathf.RoundToInt(result.value)}%");
                valueLabel.AddToClassList("result-row__value");

                var tapsLabel = new Label($"{result.tapCount} taps");
                tapsLabel.AddToClassList("result-row__taps");

                row.Add(rankLabel);
                row.Add(nameLabel);
                row.Add(platformLabel);
                row.Add(valueLabel);
                row.Add(tapsLabel);

                _resultsList.Add(row);

                int delayMs = (i + 1) * 120;
                var capturedRow = row;
                row.schedule.Execute(() =>
                {
                    capturedRow.AddToClassList("result-row--visible");
                    capturedRow.style.translate = new Translate(0f, 0f);
                    capturedRow.style.opacity = 1f;
                }).StartingIn(delayMs);
            }
        }

        if (_resultsTitle != null)
            StartCoroutine(CountdownPop(_resultsTitle, 0.6f));
    }

    private void CreatePlayerBars()
    {
        _barsContainer.Clear();
        _bars.Clear();
        _targetValues.Clear();

        if (lobbyManager.CurrentLobby?.players == null) return;

        for (int i = 0; i < lobbyManager.CurrentLobby.players.Length; i++)
        {
            var player = lobbyManager.CurrentLobby.players[i];
            var barElement = playerBarTemplate.Instantiate();
            bool isLocal = player.id == lobbyManager.PlayerId;
            var barUI = new PlayerBarUI(barElement, player.name, player.id, isLocal, i);

            _barsContainer.Add(barElement);
            _bars[player.id] = barUI;
            _targetValues[player.id] = 0f;
        }
    }

    private void ReturnToLobby()
    {
        _gameActive = false;
        _bars.Clear();
        _targetValues.Clear();
        isReady = false;
        _readyButton.text = "READY UP";
        _readyButton.SetEnabled(true);
        ShowPanel(_lobbyPanel);
    }

    private void LeaveLobby()
    {
        lobbyManager.LeaveLobby();
        _gameActive = false;
        _bars.Clear();
        _targetValues.Clear();
        isReady = false;
        _readyButton.text = "READY UP";
        _readyButton.SetEnabled(true);
        ShowPanel(_joinPanel);
        StartCoroutine(FetchLobbies());
    }

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

    private void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyUpdated -= UpdateLobbyDisplay;
            lobbyManager.OnGameStarting -= HandleGameStarting;
            lobbyManager.OnGameEvent -= HandleGameEvent;
        }
    }

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
