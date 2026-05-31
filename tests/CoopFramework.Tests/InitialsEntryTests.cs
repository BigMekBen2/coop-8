using Xunit;
using RaylibCoop;

namespace CoopFramework.Tests;

public class InitialsEntryTests
{
    private static InitialsEntry Make() => new();

    [Fact] public void Update_DownPress_AdvancesWheelForward()
    {
        var e = Make();
        e.Update(0f, false, true, false, false);
        Assert.Equal(1, e.WheelIndices[0]);
    }

    [Fact] public void Update_UpPress_AdvancesWheelBackward()
    {
        var e = Make();
        e.Update(0f, true, false, false, false);
        Assert.Equal(InitialsEntry.Wheel.Length - 1, e.WheelIndices[0]);
    }

    [Fact] public void Update_WheelWrapsForward()
    {
        var e = Make();
        int len = InitialsEntry.Wheel.Length;
        for (int i = 0; i < len; i++)
        {
            e.Update(0f, false, true,  false, false); // press
            e.Update(0f, false, false, false, false); // release
        }
        Assert.Equal(0, e.WheelIndices[0]);
    }

    [Fact] public void Update_WheelWrapsBackward()
    {
        var e = Make();
        e.Update(0f, true, false, false, false);
        Assert.Equal(InitialsEntry.Wheel.Length - 1, e.WheelIndices[0]);
    }

    // Helper: press then release (rising-edge debounce requires a release between presses)
    private static void Press(InitialsEntry e, bool confirm = false, bool back = false)
    {
        e.Update(0f, false, false, confirm, back);
        e.Update(0f, false, false, false,   false); // release
    }

    [Fact] public void Update_Confirm_AdvancesCursor()
    {
        var e = Make();
        Press(e, confirm: true);
        Assert.Equal(1, e.Cursor);
    }

    [Fact] public void Update_ConfirmAtColumn2_SetsIsComplete()
    {
        var e = Make();
        Press(e, confirm: true); // 0→1
        Press(e, confirm: true); // 1→2
        Press(e, confirm: true); // complete
        Assert.True(e.IsComplete);
    }

    [Fact] public void Update_Back_ReturnsCursorLeft()
    {
        var e = Make();
        Press(e, confirm: true);        // cursor 0→1
        Press(e, back: true);           // cursor 1→0
        Assert.Equal(0, e.Cursor);
    }

    [Fact] public void Update_BackAtColumn0_DoesNothing()
    {
        var e = Make();
        Press(e, back: true);
        Assert.Equal(0, e.Cursor);
    }

    [Fact] public void Update_AutoRepeat_FiresAfterDelay()
    {
        var e = Make();
        e.Update(0f,    false, true, false, false); // initial press → index 1
        e.Update(0.39f, false, true, false, false); // not yet
        Assert.Equal(1, e.WheelIndices[0]);
        e.Update(0.02f, false, true, false, false); // crosses 0.40s threshold → index 2
        Assert.Equal(2, e.WheelIndices[0]);
    }

    [Fact] public void Update_AutoRepeat_RepeatsAtRate()
    {
        var e = Make();
        e.Update(0f,    false, true, false, false); // index 1
        e.Update(0.40f, false, true, false, false); // first repeat → 2
        e.Update(0.08f, false, true, false, false); // second repeat → 3
        Assert.Equal(3, e.WheelIndices[0]);
    }

    [Fact] public void CurrentInitials_ReflectsWheelState()
    {
        var e = Make();
        // Default: all indices 0 → "AAA"
        Assert.Equal("AAA", e.CurrentInitials);
    }

    [Fact] public void CurrentInitials_SpaceChar_InString()
    {
        var e = Make();
        // Wheel index 26 = ' '
        for (int i = 0; i < 26; i++)
        {
            e.Update(0f, false, true,  false, false);
            e.Update(0f, false, false, false, false);
        }
        Assert.Equal(' ', e.CurrentInitials[0]);
    }
}
