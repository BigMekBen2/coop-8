using System.Numerics;

namespace RaylibCoop;

public class ControllerState
{
    public int PlayerId { get; init; }
    public bool IsKeyboard { get; init; }
    public Dictionary<string, float> Axes    { get; } = new();
    public Dictionary<string, bool>  Buttons { get; } = new();
    public Dictionary<string, bool>  DPad    { get; } = new();
    public float VibrationIntensity { get; set; }

    public ControllerState(int playerId, bool isKeyboard = false)
    {
        PlayerId   = playerId;
        IsKeyboard = isKeyboard;

        Axes["left_stick_x"]  = 0f;
        Axes["left_stick_y"]  = 0f;
        Axes["right_stick_x"] = 0f;
        Axes["right_stick_y"] = 0f;
        Axes["trigger_left"]  = 0f;
        Axes["trigger_right"] = 0f;

        Buttons["a"]               = false;
        Buttons["b"]               = false;
        Buttons["x"]               = false;
        Buttons["y"]               = false;
        Buttons["lb"]              = false;
        Buttons["rb"]              = false;
        Buttons["lt"]              = false;
        Buttons["rt"]              = false;
        Buttons["start"]           = false;
        Buttons["back"]            = false;
        Buttons["left_stick_btn"]  = false;
        Buttons["right_stick_btn"] = false;
        Buttons["guide"]           = false;

        DPad["up"]    = false;
        DPad["down"]  = false;
        DPad["left"]  = false;
        DPad["right"] = false;
    }
}

public class ControllerPoller
{
    private const float _deadzone = 0.1f;
    public event Action<int>? OnControllerConnected;
    public event Action<int>? OnControllerDisconnected;

    private readonly bool[] _wasConnected = new bool[8];

    public void Initialize() { }

    public bool IsGamepadAvailable(int index)
        => Raylib_cs.Raylib.IsGamepadAvailable(index);

    // GetGamepadName returns sbyte* in Raylib-cs 6.x and requires unsafe; not exposed publicly.
    public string GetGamepadName(int index) => "Generic Gamepad";

    public void Poll(int maxPlayers, List<ControllerState> states)
    {
        states.Clear();
        for (int id = 0; id < maxPlayers; id++)
        {
            bool connected = Raylib_cs.Raylib.IsGamepadAvailable(id);

            if (connected && !_wasConnected[id])
                OnControllerConnected?.Invoke(id);
            else if (!connected && _wasConnected[id])
                OnControllerDisconnected?.Invoke(id);

            _wasConnected[id] = connected;

            if (!connected) continue;

            var state = new ControllerState(id);
            UpdateControllerState(state);
            states.Add(state);
        }
    }

    private void UpdateControllerState(ControllerState state)
    {
        int id = state.PlayerId;

        state.Axes["left_stick_x"]  = ApplyDeadzone(Raylib_cs.Raylib.GetGamepadAxisMovement(id, Raylib_cs.GamepadAxis.LeftX));
        state.Axes["left_stick_y"]  = ApplyDeadzone(Raylib_cs.Raylib.GetGamepadAxisMovement(id, Raylib_cs.GamepadAxis.LeftY));
        state.Axes["right_stick_x"] = ApplyDeadzone(Raylib_cs.Raylib.GetGamepadAxisMovement(id, Raylib_cs.GamepadAxis.RightX));
        state.Axes["right_stick_y"] = ApplyDeadzone(Raylib_cs.Raylib.GetGamepadAxisMovement(id, Raylib_cs.GamepadAxis.RightY));
        state.Axes["trigger_left"]  = ApplyDeadzone(Raylib_cs.Raylib.GetGamepadAxisMovement(id, Raylib_cs.GamepadAxis.LeftTrigger));
        state.Axes["trigger_right"] = ApplyDeadzone(Raylib_cs.Raylib.GetGamepadAxisMovement(id, Raylib_cs.GamepadAxis.RightTrigger));

        state.Buttons["a"]               = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.RightFaceDown);
        state.Buttons["b"]               = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.RightFaceRight);
        state.Buttons["x"]               = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.RightFaceLeft);
        state.Buttons["y"]               = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.RightFaceUp);
        state.Buttons["lb"]              = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.LeftTrigger1);
        state.Buttons["rb"]              = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.RightTrigger1);
        state.Buttons["lt"]              = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.LeftTrigger2);
        state.Buttons["rt"]              = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.RightTrigger2);
        state.Buttons["start"]           = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.MiddleRight);
        state.Buttons["back"]            = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.MiddleLeft);
        state.Buttons["left_stick_btn"]  = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.LeftThumb);
        state.Buttons["right_stick_btn"] = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.RightThumb);
        state.Buttons["guide"]           = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.Middle);

        state.DPad["up"]    = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.LeftFaceUp);
        state.DPad["down"]  = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.LeftFaceDown);
        state.DPad["left"]  = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.LeftFaceLeft);
        state.DPad["right"] = Raylib_cs.Raylib.IsGamepadButtonDown(id, Raylib_cs.GamepadButton.LeftFaceRight);
    }

    public void SetVibration(int playerId, float intensity)
    {
        // SetGamepadVibration requires Raylib-cs >= 6.1; no-op if unavailable.
        // Raylib_cs.Raylib.SetGamepadVibration(playerId, intensity, intensity, 0.3f);
    }

    private static float ApplyDeadzone(float raw)
        => Math.Abs(raw) < _deadzone ? 0f : raw;
}
