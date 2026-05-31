namespace RaylibCoop;

public sealed class PlayerProfile
{
    public int PlayerId    { get; }
    public int ControllerId { get; }
    public string Initials { get; }
    public PlayerColor Color { get; }
    public bool IsKeyboard { get; }
    public string PlayerTag => $"P{PlayerId + 1}";

    public PlayerProfile(int playerId, int controllerId, string? initials, PlayerColor color, bool isKeyboard)
    {
        if (playerId < 0 || playerId > 7)
            throw new ArgumentOutOfRangeException(nameof(playerId), "Must be 0–7.");

        PlayerId     = playerId;
        ControllerId = controllerId;
        Color        = color;
        IsKeyboard   = isKeyboard;

        if (initials == null)
            Initials = "   ";
        else
        {
            string upper = initials.ToUpper();
            Initials = upper.Length >= 3
                ? upper[..3]
                : upper.PadRight(3);
        }
    }
}
