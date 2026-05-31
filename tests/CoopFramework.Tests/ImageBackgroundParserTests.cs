using Xunit;
using RaylibCoop;

namespace CoopFramework.Tests;

public class ImageBackgroundParserTests
{
    private static ImageBackground? P(Dictionary<string, string> props)
        => ImageBackgroundParser.Parse(props);

    [Fact] public void Parse_NullImagePath_ReturnsNull()
        => Assert.Null(P(new()));

    [Fact] public void Parse_EmptyImagePath_ReturnsNull()
        => Assert.Null(P(new() { { "background-image", "" } }));

    [Fact] public void Parse_ValidPath_SetsImagePath()
        => Assert.Equal("img/bg.png", P(new() { { "background-image", "img/bg.png" } })!.ImagePath);

    [Fact] public void Parse_DefaultScaleMode_IsStretch()
        => Assert.Equal(ScaleMode.Stretch, P(new() { { "background-image", "a.png" } })!.ScaleMode);

    [Fact] public void Parse_BackgroundSizeCover_IsCover()
        => Assert.Equal(ScaleMode.Cover,
            P(new() { { "background-image", "a.png" }, { "background-size", "cover" } })!.ScaleMode);

    [Fact] public void Parse_BackgroundAlignHRight_IsRight()
        => Assert.Equal(AlignH.Right,
            P(new() { { "background-image", "a.png" }, { "background-align-h", "right" } })!.AlignH);

    [Fact] public void Parse_BackgroundAlignVBottom_IsBottom()
        => Assert.Equal(AlignV.Bottom,
            P(new() { { "background-image", "a.png" }, { "background-align-v", "bottom" } })!.AlignV);

    [Fact] public void Parse_BackgroundSliceFourValues_ParsesAllInsets()
    {
        var bg = P(new() { { "background-image", "a.png" }, { "background-slice", "1 2 3 4" } })!;
        Assert.Equal(1, bg.NineSlice!.Top);
        Assert.Equal(2, bg.NineSlice.Right);
        Assert.Equal(3, bg.NineSlice.Bottom);
        Assert.Equal(4, bg.NineSlice.Left);
    }

    [Fact] public void Parse_BackgroundSliceOneValue_AllInsetsEqual()
    {
        var bg = P(new() { { "background-image", "a.png" }, { "background-slice", "8" } })!;
        Assert.Equal(8, bg.NineSlice!.Top);
        Assert.Equal(8, bg.NineSlice.Right);
        Assert.Equal(8, bg.NineSlice.Bottom);
        Assert.Equal(8, bg.NineSlice.Left);
    }

    [Fact] public void Parse_BackgroundTint_SetsHex()
        => Assert.Equal("FF0000",
            P(new() { { "background-image", "a.png" }, { "background-tint", "FF0000" } })!.Tint);

    [Fact] public void Parse_BackgroundOpacityOutOfRange_Clamped()
    {
        var bg = P(new() { { "background-image", "a.png" }, { "background-opacity", "2.5" } })!;
        Assert.Equal(1f, bg.Opacity);
    }

    [Fact] public void Parse_BackgroundLayerBelow_DrawAboveColorFalse()
        => Assert.False(P(new() { { "background-image", "a.png" }, { "background-layer", "below" } })!.DrawAboveColor);
}
