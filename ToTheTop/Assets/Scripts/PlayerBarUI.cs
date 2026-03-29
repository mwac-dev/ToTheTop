using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for a single player's vertical bar.
/// </summary>
public class PlayerBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private Image barFill;
    [SerializeField] private Image barBackground;
    [SerializeField] private TextMeshProUGUI platformIcon;
    [SerializeField] private TextMeshProUGUI tapCountLabel;

    [Header("Colors")]
    [SerializeField] private Color localPlayerColor = new Color(0.2f, 0.8f, 0.4f);   // green
    [SerializeField] private Color otherPlayerColor = new Color(0.3f, 0.5f, 0.9f);    // blue
    [SerializeField] private Color localHighlightBg = new Color(0.15f, 0.25f, 0.15f); // subtle green bg

    public float CurrentValue { get; set; }

    private string _playerId;
    private bool _isLocal;

    /// <summary>
    /// Initialize the bar with player info.
    /// </summary>
    public void Setup(string playerName, string playerId, bool isLocal)
    {
        _playerId = playerId;
        _isLocal = isLocal;

        nameLabel.text = playerName;
        barFill.fillAmount = 0f;
        CurrentValue = 0f;

        barFill.color = isLocal ? localPlayerColor : otherPlayerColor;

        // Highlight local player's bar background
        if (isLocal && barBackground != null)
        {
            barBackground.color = localHighlightBg;
        }

        // Show "YOU" indicator for local player
        if (isLocal)
        {
            nameLabel.text = $"{playerName} (YOU)";
            nameLabel.fontStyle = FontStyles.Bold;
        }

        if (tapCountLabel != null)
            tapCountLabel.text = "0 taps";
    }

    /// <summary>
    /// Set the platform icon text.
    /// Called when we receive platform info from the server.
    /// </summary>
    public void SetPlatform(string platform)
    {
        if (platformIcon != null)
            platformIcon.text = platform == "browser" ? "[WEB]" : "[PC]";
    }

    /// <summary>
    /// Update the visual fill of the bar. Value is 0-1.
    /// </summary>
    public void UpdateFill(float normalizedValue)
    {
        barFill.fillAmount = Mathf.Clamp01(normalizedValue);
    }

    /// <summary>
    /// Update the tap count display.
    /// </summary>
    public void SetTapCount(int count)
    {
        if (tapCountLabel != null)
            tapCountLabel.text = $"{count} taps";
    }
}
