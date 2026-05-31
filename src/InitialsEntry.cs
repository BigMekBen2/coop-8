namespace RaylibCoop;

public class InitialsEntry
{
    public static readonly string Wheel =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ -.";   // indices 0-28

    public int[] WheelIndices { get; } = new int[3];
    public int Cursor { get; private set; }
    public bool IsComplete { get; private set; }

    private const float AutoRepeatDelay = 0.40f;
    private const float AutoRepeatRate  = 0.08f;

    private float _holdTimer;
    private float _nextFireTime;
    private bool  _holdingScroll;
    private int   _scrollDir;

    public string CurrentInitials =>
        new(WheelIndices.Select(i => Wheel[i]).ToArray());

    public void Update(float dt, bool scrollUp, bool scrollDown, bool confirm, bool back)
    {
        if (IsComplete) return;

        int scroll = scrollDown ? 1 : scrollUp ? -1 : 0;

        if (scroll != 0)
        {
            if (!_holdingScroll || _scrollDir != scroll)
            {
                // Fresh press
                ApplyScroll(scroll);
                _holdTimer     = 0f;
                _nextFireTime  = AutoRepeatDelay;
                _holdingScroll = true;
                _scrollDir     = scroll;
            }
            else
            {
                _holdTimer += dt;
                if (_holdTimer >= _nextFireTime)
                {
                    ApplyScroll(scroll);
                    _nextFireTime += AutoRepeatRate;
                }
            }
        }
        else
        {
            _holdingScroll = false;
            _scrollDir     = 0;
            _holdTimer     = 0f;
            _nextFireTime  = float.MaxValue;
        }

        if (confirm)
        {
            if (Cursor < 2)
                Cursor++;
            else
                IsComplete = true;
        }

        if (back && !confirm)
        {
            if (Cursor > 0) Cursor--;
        }
    }

    private void ApplyScroll(int dir)
    {
        int idx = WheelIndices[Cursor] + dir;
        WheelIndices[Cursor] = ((idx % Wheel.Length) + Wheel.Length) % Wheel.Length;
    }
}
