namespace RaylibCoop;

public class InputManager
{
    public int MaxPlayers { get; }

    public event Action<int>? OnControllerConnected;
    public event Action<int>? OnControllerDisconnected;

    public KeyboardFallback KeyboardFallback => _keyboardFallback;

    private readonly ControllerPoller _poller = new();
    private readonly KeyboardFallback _keyboardFallback = new();
    private readonly OverlayRenderer _overlay = new();
    private bool _initialized;
    private bool _keyboardEnabled = true;

    private readonly List<ControllerState> _lastStates = new();

    public InputManager(int maxPlayers = 4)
    {
        if (maxPlayers < 1 || maxPlayers > 8)
            throw new ArgumentOutOfRangeException(nameof(maxPlayers), "Must be 1–8.");
        MaxPlayers = maxPlayers;
    }

    public void Initialize()
    {
        _poller.Initialize();
        _keyboardFallback.Initialize();

        _poller.OnControllerConnected    += id => OnControllerConnected?.Invoke(id);
        _poller.OnControllerDisconnected += id => OnControllerDisconnected?.Invoke(id);

        _initialized = true;
    }

    public void LoadOverlayCSS(string path) => _overlay.LoadCSS(path);
    public void LoadOverlayCSSFromString(string css) => _overlay.LoadCSSFromString(css);

    public Dictionary<int, ControllerState> ReadControllers()
    {
        if (!_initialized)
            throw new InvalidOperationException("Call Initialize() first.");

        _poller.Poll(MaxPlayers, _lastStates);
        _keyboardFallback.PollKeyboard();

        var result = new Dictionary<int, ControllerState>();
        foreach (var s in _lastStates)
            result[s.PlayerId] = s;

        if (_keyboardEnabled)
        {
            var kb = _keyboardFallback.GetInput();
            if (kb != null)
                result[-1] = kb;
        }

        return result;
    }

    public bool IsPlayerActive(int playerId) =>
        _lastStates.Any(s => s.PlayerId == playerId);

    public List<int> GetActivePlayerIds() =>
        _lastStates.Select(s => s.PlayerId).ToList();

    public int GetConnectedControllerCount() => _lastStates.Count;

    public bool HasAnyControllerConnected() => _lastStates.Count > 0;

    public bool IsKeyboardFallbackEnabled() => _keyboardEnabled;

    public void SetKeyboardFallbackEnabled(bool enabled)
    {
        _keyboardEnabled = enabled;
        _keyboardFallback.SetEnabled(enabled);
    }

    public void SetVibration(int playerId, float intensity)
        => _poller.SetVibration(playerId, intensity);

    public void RenderOverlays() => _overlay.RenderAll(_lastStates);

    public void Shutdown() => _overlay.UnloadAll();
}
