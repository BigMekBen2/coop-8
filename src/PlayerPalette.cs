namespace RaylibCoop;

public static class PlayerPalette
{
    public static readonly PlayerColor[] Colors =
    [
        new(255,  68,  68, 255),  // P1 red
        new( 68, 170, 255, 255),  // P2 blue
        new(255, 255,  68, 255),  // P3 yellow
        new( 68, 255, 136, 255),  // P4 green
        new(255, 136, 255, 255),  // P5 magenta
        new(255, 136,   0, 255),  // P6 orange
        new(136, 255, 255, 255),  // P7 cyan
        new(255, 255, 255, 255),  // P8 white
    ];

    public static readonly PlayerColor CrtGreen = new( 57, 255,  20, 255);
    public static readonly PlayerColor DarkGreen = new(  0,  17,   0, 255);
}

public readonly record struct PlayerColor(byte R, byte G, byte B, byte A)
{
    public Raylib_cs.Color ToRaylib() => new Raylib_cs.Color(R, G, B, A);
}
