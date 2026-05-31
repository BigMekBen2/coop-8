using Xunit;
using RaylibCoop;

namespace CoopFramework.Tests;

public class InputManagerTests
{
    [Fact] public void Constructor_DefaultMaxPlayers_Is4()
        => Assert.Equal(4, new InputManager().MaxPlayers);

    [Fact] public void Constructor_MaxPlayers_8_IsValid()
        => new InputManager(maxPlayers: 8); // no exception

    [Fact] public void Constructor_MaxPlayers_9_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new InputManager(maxPlayers: 9));

    [Fact] public void Constructor_MaxPlayers_0_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new InputManager(maxPlayers: 0));

    [Fact] public void ReadControllers_BeforeInit_Throws()
    {
        var mgr = new InputManager();
        Assert.Throws<InvalidOperationException>(() => mgr.ReadControllers());
    }

    [Fact] public void KeyboardFallbackEnabled_DefaultIsTrue()
        => Assert.True(new InputManager().IsKeyboardFallbackEnabled());

    [Fact] public void SetKeyboardFallbackEnabled_False_Stores()
    {
        var mgr = new InputManager();
        mgr.SetKeyboardFallbackEnabled(false);
        Assert.False(mgr.IsKeyboardFallbackEnabled());
    }
}
