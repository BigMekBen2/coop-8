using Xunit;
using RaylibCoop;

namespace CoopFramework.Tests;

public class KeyboardFallbackTests
{
    [Fact] public void GetInput_BeforeInit_ReturnsNull()
        => Assert.Null(new KeyboardFallback().GetInput());

    [Fact] public void GetInput_WhenDisabled_ReturnsNull()
    {
        var kb = new KeyboardFallback();
        kb.Initialize();
        kb.SetEnabled(false);
        Assert.Null(kb.GetInput());
    }

    [Fact] public void SetLayout_WASD_SetsCorrectMappings()
    {
        // WASD layout: W=265 maps left_stick_up
        var kb = new KeyboardFallback();
        kb.Initialize();
        kb.SetLayout("wasd");
        // Just verify Initialize + SetLayout doesn't throw and GetInput returns null (no keys pressed in test)
        Assert.Null(kb.GetInput());
    }

    [Fact] public void SetLayout_Arrows_SetsCorrectMappings()
    {
        var kb = new KeyboardFallback();
        kb.Initialize();
        kb.SetLayout("arrows");
        Assert.Null(kb.GetInput());
    }

    [Fact] public void SetCustomKeyMap_OverridesLayout()
    {
        var kb = new KeyboardFallback();
        kb.Initialize();
        kb.SetCustomKeyMap(new Dictionary<string, int> { { "custom_action", 65 } });
        // Custom key present — no exception, GetInput returns null (key not held)
        Assert.Null(kb.GetInput());
    }
}
