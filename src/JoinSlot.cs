namespace RaylibCoop;

public class JoinSlot
{
    public int PlayerId;
    public int ControllerId;
    public bool IsKeyboard;
    public InitialsEntry Entry = new();

    // Grid position (col 0-3, row 0-1)
    public int GridCol;
    public int GridRow;

    public float AppearProgress;   // 0 → 1 over ~0.15s
    public float DismissProgress;  // 1 → 0 over ~0.18s
    public float InputGrace = 0.25f; // seconds before input is accepted (prevents trigger button from confirming)

    public bool ReadyToRemove => DismissProgress <= 0f && Entry.IsComplete;
}
