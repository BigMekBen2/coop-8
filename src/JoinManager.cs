namespace RaylibCoop;

public class JoinManager
{
    public event Action<PlayerProfile>? OnPlayerJoined;
    public event Action<int>? OnPlayerLeft;

    public bool IsAccepting => _accepting;
    public int ConfirmedCount { get; private set; }

    private readonly InputManager _inputMgr;
    private readonly int _screenW;
    private readonly int _screenH;
    private readonly int _maxPlayers;

    private bool _accepting;

    // playerId → slot
    private readonly Dictionary<int, JoinSlot> _slots = new();
    // controllerId → playerId
    private readonly Dictionary<int, int> _controllerToSlot = new();
    // which grid cells are occupied: [col, row]
    private readonly bool[,] _gridUsed = new bool[4, 2];
    // which playerIds are in use
    private readonly bool[] _playerIdUsed = new bool[8];

    private readonly JoinPopupRenderer _renderer;

    public int ActiveEntryCount => _slots.Count;

    public JoinManager(InputManager inputManager, int screenWidth, int screenHeight, int maxPlayers = 8)
    {
        _inputMgr   = inputManager;
        _screenW    = screenWidth;
        _screenH    = screenHeight;
        _maxPlayers = Math.Min(maxPlayers, 8);
        _renderer   = new JoinPopupRenderer(screenWidth, screenHeight);
    }

    public void Open()  => _accepting = true;
    public void Close() => _accepting = false;

    public void Update(float dt)
        => UpdateWithInput(dt, _inputMgr.ReadControllers());

    public void UpdateWithInput(float dt, Dictionary<int, ControllerState> states)
    {
        if (_accepting)
            DetectNewJoins(states);

        foreach (var slot in _slots.Values.ToList())
        {
            if (slot.Entry.IsComplete)
            {
                slot.DismissProgress -= dt / 0.18f;
                slot.AppearProgress   = slot.DismissProgress;
                if (slot.ReadyToRemove) FinalizeSlot(slot);
                continue;
            }

            if (slot.AppearProgress < 1f)
                slot.AppearProgress = Math.Min(slot.AppearProgress + dt / 0.15f, 1f);

            var cs = GetControllerState(slot, states);
            if (cs != null)
                RouteInput(slot, cs, dt);
        }

        // Disconnect detection
        foreach (var (controllerId, playerId) in _controllerToSlot.ToList())
        {
            if (!states.ContainsKey(controllerId) && _slots.ContainsKey(playerId))
            {
                var slot = _slots[playerId];
                if (!slot.Entry.IsComplete)
                    RemoveSlot(playerId, disconnected: true);
            }
        }
    }

    public void Render() => _renderer.Render(_slots.Values.ToList(), _accepting);

    // ── Private ─────────────────────────────────────────────────────────

    private void DetectNewJoins(Dictionary<int, ControllerState> states)
    {
        foreach (var (controllerId, state) in states)
        {
            if (_controllerToSlot.ContainsKey(controllerId)) continue;
            if (_slots.Count >= _maxPlayers) break;

            bool anyButton = state.Buttons.Values.Any(v => v)
                          || state.DPad.Values.Any(v => v);
            if (!anyButton) continue;

            CreateSlot(controllerId, state.IsKeyboard);
        }
    }

    private void CreateSlot(int controllerId, bool isKeyboard)
    {
        int playerId = NextFreePlayerId();
        if (playerId < 0) return;

        var (col, row) = NextFreeGridCell();
        if (col < 0) return;

        _playerIdUsed[playerId]  = true;
        _gridUsed[col, row]      = true;
        _controllerToSlot[controllerId] = playerId;

        _slots[playerId] = new JoinSlot
        {
            PlayerId     = playerId,
            ControllerId = controllerId,
            IsKeyboard   = isKeyboard,
            GridCol      = col,
            GridRow      = row,
            AppearProgress  = 0f,
            DismissProgress = 1f,
        };
    }

    private void RouteInput(JoinSlot slot, ControllerState cs, float dt)
    {
        bool up      = cs.DPad["up"]    || cs.Axes["left_stick_y"] < -0.5f;
        bool down    = cs.DPad["down"]  || cs.Axes["left_stick_y"] >  0.5f;
        bool confirm = cs.DPad["right"] || cs.Buttons["a"] || cs.Buttons["start"];
        bool back    = cs.DPad["left"]  || cs.Buttons["b"];

        slot.Entry.Update(dt, scrollUp: up, scrollDown: down, confirm: confirm, back: back);
    }

    private ControllerState? GetControllerState(JoinSlot slot, Dictionary<int, ControllerState> states)
        => states.TryGetValue(slot.ControllerId, out var cs) ? cs : null;

    private void FinalizeSlot(JoinSlot slot)
    {
        var color   = PlayerPalette.Colors[slot.PlayerId % PlayerPalette.Colors.Length];
        var profile = new PlayerProfile(slot.PlayerId, slot.Entry.CurrentInitials, color, slot.IsKeyboard);

        _slots.Remove(slot.PlayerId);
        _controllerToSlot.Remove(slot.ControllerId);
        _gridUsed[slot.GridCol, slot.GridRow] = false;
        // intentionally keep _playerIdUsed[playerId] = true — slot is confirmed, not freed

        ConfirmedCount++;
        OnPlayerJoined?.Invoke(profile);
    }

    private void RemoveSlot(int playerId, bool disconnected)
    {
        if (!_slots.TryGetValue(playerId, out var slot)) return;

        _slots.Remove(playerId);
        _controllerToSlot.Remove(slot.ControllerId);
        _gridUsed[slot.GridCol, slot.GridRow] = false;
        _playerIdUsed[playerId] = false;

        if (disconnected)
            OnPlayerLeft?.Invoke(playerId);
    }

    private int NextFreePlayerId()
    {
        for (int i = 0; i < _maxPlayers; i++)
            if (!_playerIdUsed[i]) return i;
        return -1;
    }

    private (int col, int row) NextFreeGridCell()
    {
        for (int row = 0; row < 2; row++)
        for (int col = 0; col < 4; col++)
            if (!_gridUsed[col, row]) return (col, row);
        return (-1, -1);
    }
}
