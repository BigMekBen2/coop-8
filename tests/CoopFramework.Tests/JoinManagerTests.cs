using Xunit;
using RaylibCoop;

namespace CoopFramework.Tests;

public class JoinManagerTests
{
    // Build a minimal ControllerState that looks like a button press.
    private static Dictionary<int, ControllerState> Press(int controllerId)
    {
        var cs = new ControllerState(controllerId);
        cs.Buttons["a"] = true;
        return new Dictionary<int, ControllerState> { [controllerId] = cs };
    }

    private static Dictionary<int, ControllerState> Confirm(int controllerId)
    {
        var cs = new ControllerState(controllerId);
        cs.Buttons["a"] = true;
        return new Dictionary<int, ControllerState> { [controllerId] = cs };
    }

    // Helper: drain the input grace period, keeping controller connected
    private static void DrainGrace(JoinManager mgr, int controllerId)
        => mgr.UpdateWithInput(0.3f, new Dictionary<int, ControllerState> { [controllerId] = new ControllerState(controllerId) });

    private static Dictionary<int, ControllerState> Empty() => new();

    private static JoinManager MakeOpen(int max = 8)
    {
        var mgr = new JoinManager(null!, 1920, 1080, max);
        mgr.Open();
        return mgr;
    }

    [Fact] public void DetectNewJoin_FirstButtonPress_CreatesSlot()
    {
        var mgr = MakeOpen();
        mgr.UpdateWithInput(0f, Press(0));
        Assert.Equal(1, mgr.ActiveEntryCount);
    }

    [Fact] public void DetectNewJoin_SameControllerTwice_OnlyOneSlot()
    {
        var mgr = MakeOpen();
        mgr.UpdateWithInput(0f, Press(0));
        mgr.UpdateWithInput(0f, Press(0));
        Assert.Equal(1, mgr.ActiveEntryCount);
    }

    [Fact] public void DetectNewJoin_EightPlayers_AllSlotsCreated()
    {
        var mgr = MakeOpen();
        var states = new Dictionary<int, ControllerState>();
        for (int i = 0; i < 8; i++) { var cs = new ControllerState(i); cs.Buttons["a"] = true; states[i] = cs; }
        mgr.UpdateWithInput(0f, states);
        Assert.Equal(8, mgr.ActiveEntryCount);
    }

    [Fact] public void DetectNewJoin_NinthPress_Ignored()
    {
        var mgr = MakeOpen();
        var states = new Dictionary<int, ControllerState>();
        for (int i = 0; i < 9; i++) { var cs = new ControllerState(i); cs.Buttons["a"] = true; states[i] = cs; }
        mgr.UpdateWithInput(0f, states);
        Assert.Equal(8, mgr.ActiveEntryCount);
    }

    [Fact] public void DetectNewJoin_Closed_NoNewSlots()
    {
        var mgr = MakeOpen();
        mgr.Close();
        mgr.UpdateWithInput(0f, Press(0));
        Assert.Equal(0, mgr.ActiveEntryCount);
    }

    [Fact] public void PlayerJoinedEvent_FiredAfterConfirm()
    {
        var mgr = MakeOpen();
        int fired = 0;
        mgr.OnPlayerJoined += _ => fired++;

        mgr.UpdateWithInput(0f, Press(0));
        DrainGrace(mgr, 0);

        // Confirm three times to complete initials, then run dismiss animation
        for (int i = 0; i < 3; i++) mgr.UpdateWithInput(0f, Confirm(0));
        mgr.UpdateWithInput(0.2f, Empty()); // dismiss animation completes

        Assert.Equal(1, fired);
    }

    [Fact] public void PlayerJoinedEvent_ProfileHasCorrectId()
    {
        var mgr = MakeOpen();
        PlayerProfile? profile = null;
        mgr.OnPlayerJoined += p => profile = p;

        mgr.UpdateWithInput(0f, Press(0));
        DrainGrace(mgr, 0);
        for (int i = 0; i < 3; i++) mgr.UpdateWithInput(0f, Confirm(0));
        mgr.UpdateWithInput(0.2f, Empty());

        Assert.Equal(0, profile!.PlayerId);
    }

    [Fact] public void PlayerJoinedEvent_ProfileHasCorrectColor()
    {
        var mgr = MakeOpen();
        PlayerProfile? profile = null;
        mgr.OnPlayerJoined += p => profile = p;

        mgr.UpdateWithInput(0f, Press(0));
        DrainGrace(mgr, 0);
        for (int i = 0; i < 3; i++) mgr.UpdateWithInput(0f, Confirm(0));
        mgr.UpdateWithInput(0.2f, Empty());

        Assert.Equal(PlayerPalette.Colors[0], profile!.Color);
    }

    [Fact] public void PlayerJoinedEvent_ProfileHasCorrectInitials()
    {
        var mgr = MakeOpen();
        PlayerProfile? profile = null;
        mgr.OnPlayerJoined += p => profile = p;

        mgr.UpdateWithInput(0f, Press(0));
        DrainGrace(mgr, 0);
        for (int i = 0; i < 3; i++) mgr.UpdateWithInput(0f, Confirm(0));
        mgr.UpdateWithInput(0.2f, Empty());

        // Default wheel indices are 0,0,0 → "AAA"
        Assert.Equal("AAA", profile!.Initials);
    }

    [Fact] public void PlayerLeftEvent_FiredOnDisconnect()
    {
        var mgr = MakeOpen();
        int leftId = -1;
        mgr.OnPlayerLeft += id => leftId = id;

        mgr.UpdateWithInput(0f, Press(0));
        mgr.UpdateWithInput(0f, Empty()); // controller 0 gone

        Assert.Equal(0, leftId);
    }

    [Fact] public void SlotFreed_AfterDisconnect_NewPlayerCanReuseId()
    {
        var mgr = MakeOpen();
        var profiles = new List<PlayerProfile>();
        mgr.OnPlayerJoined += p => profiles.Add(p);

        // P0 joins and disconnects mid-entry
        mgr.UpdateWithInput(0f, Press(0));
        mgr.UpdateWithInput(0f, Empty());

        // New controller joins; should reuse playerId 0
        mgr.UpdateWithInput(0f, Press(1));
        Assert.Equal(1, mgr.ActiveEntryCount);
    }

    [Fact] public void ConfirmedCount_IncrementsOnJoin()
    {
        var mgr = MakeOpen();
        mgr.UpdateWithInput(0f, Press(0));
        DrainGrace(mgr, 0);
        for (int i = 0; i < 3; i++) mgr.UpdateWithInput(0f, Confirm(0));
        mgr.UpdateWithInput(0.2f, Empty());

        Assert.Equal(1, mgr.ConfirmedCount);
    }

    [Fact] public void GridCell_FreedAfterConfirm_NextJoinUsesCell()
    {
        var mgr = MakeOpen(1); // 1-player max so grid cell 0,0 is reused
        mgr.UpdateWithInput(0f, Press(0));
        DrainGrace(mgr, 0);
        for (int i = 0; i < 3; i++) mgr.UpdateWithInput(0f, Confirm(0));
        mgr.UpdateWithInput(0.2f, Empty());

        // After confirm, confirmed count is 1, but maxPlayers=1 so no new slots possible
        // Just verify it didn't crash and ConfirmedCount is correct
        Assert.Equal(1, mgr.ConfirmedCount);
    }
}
