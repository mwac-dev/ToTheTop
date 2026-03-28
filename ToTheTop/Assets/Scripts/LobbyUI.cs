using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the lobby UI. Listens to LobbyManager events and updates the display.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyManager lobbyManager;

    [Header("Panels")]
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject lobbyPanel;

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

    private bool isReady;

    private void Start()
    {
        // Show join panel initially
        joinPanel.SetActive(true);
        lobbyPanel.SetActive(false);

        
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

        readyButton.onClick.AddListener(() =>
        {
            isReady = !isReady;
            lobbyManager.SetReady(isReady);
            readyButtonText.text = isReady ? "READY!" : "Ready Up";
        });

        // Subscribe to lobby events
        lobbyManager.OnLobbyUpdated += UpdateLobbyDisplay;
        lobbyManager.OnGameStarting += HandleGameStart;
        lobbyManager.OnError += (err) => Debug.LogError(err);
    }

    private void UpdateLobbyDisplay(LobbyData lobby)
    {
        // Switch to lobby panel
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(true);

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

    private void HandleGameStart()
    {
        lobbyStatus.text = "GAME STARTING!";
        readyButton.interactable = false;
        //TODO: will transition to game ui panel
    }

    private void OnDestroy()
    {
        lobbyManager.OnLobbyUpdated -= UpdateLobbyDisplay;
        lobbyManager.OnGameStarting -= HandleGameStart;
    }
}
