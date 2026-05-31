using Xunit;
using RaylibCoop;

namespace CoopFramework.Tests;

public class PlayerProfileTests
{
    private static PlayerColor AnyColor => PlayerPalette.Colors[0];

    [Fact] public void Constructor_InvalidPlayerId_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerProfile(-1, 0, "ABC", AnyColor, false));

    [Fact] public void Constructor_NullInitials_PadsToSpaces()
        => Assert.Equal("   ", new PlayerProfile(0, 0, null, AnyColor, false).Initials);

    [Fact] public void Constructor_ShortInitials_PadsRight()
        => Assert.Equal("A  ", new PlayerProfile(0, 0, "A", AnyColor, false).Initials);

    [Fact] public void Constructor_LongInitials_TruncatesTo3()
        => Assert.Equal("ABC", new PlayerProfile(0, 0, "ABCD", AnyColor, false).Initials);

    [Fact] public void Constructor_Lowercase_UppercasesInitials()
        => Assert.Equal("ABC", new PlayerProfile(0, 0, "abc", AnyColor, false).Initials);

    [Fact] public void PlayerTag_ReturnsCorrectFormat()
        => Assert.Equal("P3", new PlayerProfile(2, 0, "AAA", AnyColor, false).PlayerTag);

    [Fact] public void Constructor_StoresControllerId()
        => Assert.Equal(3, new PlayerProfile(0, 3, "AAA", AnyColor, false).ControllerId);
}
