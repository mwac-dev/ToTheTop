using UnityEngine;
using UnityEngine.UIElements;

public class PlayerBarUI
{
    private readonly VisualElement _root;
    private readonly Label _handleIcon;
    private readonly Label _nameLabel;
    private readonly VisualElement _fill;
    private readonly VisualElement _stripeOverlay;
    private readonly VisualElement _track;
    private readonly Label _platformIcon;
    private readonly Label _tapCountLabel;
    private readonly bool _isLocal;

    private float _stripeOffset;

    public float CurrentValue { get; set; }

    private static readonly string[] HandleIcons = { "\U0001F984", "\U0001F916", "\U0001F9CC", "\U0001F47D" };

    public PlayerBarUI(VisualElement root, string playerName, string playerId, bool isLocal, int playerIndex)
    {
        _root = root;
        _isLocal = isLocal;
        _handleIcon = root.Q<Label>("handleIcon");
        _nameLabel = root.Q<Label>("nameLabel");
        _fill = root.Q("fill");
        _stripeOverlay = root.Q("stripeOverlay");
        _track = root.Q(className: "player-bar__track");
        _platformIcon = root.Q<Label>("platformIcon");
        _tapCountLabel = root.Q<Label>("tapCount");

        CurrentValue = 0f;
        UpdateFill(0f);

        if (_stripeOverlay != null)
        {
            for (int i = 0; i < 40; i++)
            {
                var line = new VisualElement();
                line.AddToClassList("player-bar__stripe-line");
                _stripeOverlay.Add(line);
            }
        }

        if (_handleIcon != null)
            _handleIcon.text = HandleIcons[playerIndex % HandleIcons.Length];

        _fill.AddToClassList(isLocal ? "player-bar__fill--local" : "player-bar__fill--other");

        _nameLabel.text = playerName;
        if (isLocal)
        {
            _root.Q(className: "player-bar").AddToClassList("player-bar--local");
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        if (_tapCountLabel != null)
            _tapCountLabel.text = "0 taps";
    }

    public void SetPlatform(string platform)
    {
        if (_platformIcon != null)
            _platformIcon.text = platform == "browser" ? "\U0001F310" : "\U0001F5A5\uFE0F";
    }

    public void UpdateFill(float normalizedValue)
    {
        float clamped = Mathf.Clamp01(normalizedValue);
        _fill.style.height = Length.Percent(clamped * 100f);

        if (_stripeOverlay != null)
            _stripeOverlay.style.height = Length.Percent(clamped * 100f);
    }

    public void SetTapCount(int count)
    {
        if (_tapCountLabel != null)
            _tapCountLabel.text = $"{count} taps";
    }

    public void ApplyShake(float value)
    {
        if (_track == null) return;

        float intensity = 0f;
        float speed = 30f;

        if (value >= 90f)
        {
            intensity = 3.5f;
            speed = 45f;
        }
        else if (value >= 75f)
        {
            intensity = 2f;
            speed = 35f;
        }
        else if (value >= 50f)
        {
            intensity = 0.8f;
            speed = 25f;
        }

        if (intensity > 0f)
        {
            float offsetX = Mathf.Sin(Time.time * speed) * intensity;
            float offsetY = Mathf.Cos(Time.time * speed * 0.85f) * intensity * 0.4f;
            _track.style.translate = new Translate(offsetX, offsetY);
        }
        else
        {
            _track.style.translate = new Translate(0, 0);
        }

        UpdateGlow(value);
    }

    public void AnimateStripes(float deltaTime, float value)
    {
        if (_stripeOverlay == null) return;

        float scrollSpeed = 20f;
        if (value >= 90f) scrollSpeed = 55f;
        else if (value >= 50f) scrollSpeed = 35f;

        _stripeOffset += deltaTime * scrollSpeed;
        if (_stripeOffset > 12f) _stripeOffset -= 12f;

        _stripeOverlay.style.top = new Length(-_stripeOffset, LengthUnit.Pixel);
    }

    private void UpdateGlow(float value)
    {
        if (_track == null) return;

        if (value >= 90f)
        {
            string glowClass = _isLocal ? "player-bar__track--glow-local" : "player-bar__track--glow-other";
            string otherGlow = _isLocal ? "player-bar__track--glow-other" : "player-bar__track--glow-local";
            _track.RemoveFromClassList(otherGlow);
            if (!_track.ClassListContains(glowClass))
                _track.AddToClassList(glowClass);
        }
        else
        {
            _track.RemoveFromClassList("player-bar__track--glow-local");
            _track.RemoveFromClassList("player-bar__track--glow-other");
        }
    }
}
