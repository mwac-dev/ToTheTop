using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI component for a single player's bar. Plain C# class wrapping a VisualElement.
/// </summary>
public class PlayerBarUI
{
    private readonly VisualElement _root;
    private readonly Label _nameLabel;
    private readonly VisualElement _fill;
    private readonly Label _platformIcon;
    private readonly Label _tapCountLabel;

    public float CurrentValue { get; set; }

    public PlayerBarUI(VisualElement root, string playerName, string playerId, bool isLocal)
    {
        _root = root;
        _nameLabel = root.Q<Label>("nameLabel");
        _fill = root.Q("fill");
        _platformIcon = root.Q<Label>("platformIcon");
        _tapCountLabel = root.Q<Label>("tapCount");

        CurrentValue = 0f;
        UpdateFill(0f);

        // Color via USS class
        _fill.AddToClassList(isLocal ? "player-bar__fill--local" : "player-bar__fill--other");

        if (isLocal)
        {
            _root.Q(className: "player-bar").AddToClassList("player-bar--local");
            _nameLabel.text = $"{playerName} (YOU)";
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
        else
        {
            _nameLabel.text = playerName;
        }

        if (_tapCountLabel != null)
            _tapCountLabel.text = "0 taps";
    }

    public void SetPlatform(string platform)
    {
        if (_platformIcon != null)
            _platformIcon.text = platform == "browser" ? "[WEB]" : "[PC]";
    }

    public void UpdateFill(float normalizedValue)
    {
        _fill.style.height = Length.Percent(Mathf.Clamp01(normalizedValue) * 100f);
    }

    public void SetTapCount(int count)
    {
        if (_tapCountLabel != null)
            _tapCountLabel.text = $"{count} taps";
    }
}
