namespace RaylibCoop;

public class KeyboardFallback
{
    public event Action? OnEscapePressed;

    private bool _initialized;
    private bool _enabled = true;
    private Dictionary<string, int> _customKeyMap = new();
    private Dictionary<string, bool> _keyPressedThisFrame = new();

    // WASD layout key codes (Raylib KeyboardKey values)
    private static readonly Dictionary<string, int> WasdMap = new()
    {
        { "left_stick_up",    265 }, // Up
        { "left_stick_down",  264 }, // Down
        { "left_stick_left",  263 }, // Left
        { "left_stick_right", 262 }, // Right
        { "a",                32  }, // Space
        { "b",                257 }, // Enter
        { "start",            257 }, // Enter
        { "back",             256 }, // Escape
    };

    // Arrow layout key codes
    private static readonly Dictionary<string, int> ArrowMap = new()
    {
        { "left_stick_up",    87  }, // W
        { "left_stick_down",  83  }, // S
        { "left_stick_left",  65  }, // A
        { "left_stick_right", 68  }, // D
        { "a",                32  }, // Space
        { "b",                257 }, // Enter
        { "start",            257 }, // Enter
        { "back",             256 }, // Escape
    };

    public void Initialize()
    {
        _initialized = true;
        SetLayout("wasd");
    }

    public void SetEnabled(bool enabled) => _enabled = enabled;
    public bool IsEnabled => _enabled;

    public void SetLayout(string layout)
    {
        _customKeyMap = layout.ToLower() switch
        {
            "arrows" => new Dictionary<string, int>(ArrowMap),
            _        => new Dictionary<string, int>(WasdMap),
        };
        _keyPressedThisFrame.Clear();
        foreach (var key in _customKeyMap.Keys)
            _keyPressedThisFrame[key] = false;
    }

    public void SetCustomKeyMap(Dictionary<string, int> map)
    {
        _customKeyMap = new Dictionary<string, int>(map);
        _keyPressedThisFrame.Clear();
        foreach (var key in _customKeyMap.Keys)
            _keyPressedThisFrame[key] = false;
    }

    public void PollKeyboard()
    {
        if (!_initialized || !_enabled) return;

        foreach (var kvp in _customKeyMap)
        {
            bool isPressed = Raylib_cs.Raylib.IsKeyDown((Raylib_cs.KeyboardKey)kvp.Value);
            _keyPressedThisFrame[kvp.Key] = isPressed;
        }

        if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Escape))
            OnEscapePressed?.Invoke();
    }

    public ControllerState? GetInput()
    {
        if (!_initialized || !_enabled) return null;

        var state = new ControllerState(playerId: -1, isKeyboard: true);

        if (_keyPressedThisFrame.TryGetValue("left_stick_up",    out bool up))    state.DPad["up"]    = up;
        if (_keyPressedThisFrame.TryGetValue("left_stick_down",  out bool down))  state.DPad["down"]  = down;
        if (_keyPressedThisFrame.TryGetValue("left_stick_left",  out bool left))  state.DPad["left"]  = left;
        if (_keyPressedThisFrame.TryGetValue("left_stick_right", out bool right)) state.DPad["right"] = right;

        if (up)    state.Axes["left_stick_y"] = -1f;
        if (down)  state.Axes["left_stick_y"] =  1f;
        if (left)  state.Axes["left_stick_x"] = -1f;
        if (right) state.Axes["left_stick_x"] =  1f;

        foreach (var btn in new[] { "a", "b", "x", "y", "start", "back", "lb", "rb" })
        {
            if (_keyPressedThisFrame.TryGetValue(btn, out bool pressed))
                state.Buttons[btn] = pressed;
        }

        bool anyInput = state.DPad.Values.Any(v => v)
                     || state.Buttons.Values.Any(v => v)
                     || state.Axes.Values.Any(v => v != 0f);

        return anyInput ? state : null;
    }
}
