using Xunit;
using RaylibCoop;

namespace CoopFramework.Tests;

public class OverlayRendererTests
{
    private static OverlayRenderer Make() => new();

    [Fact] public void LoadCSSFromString_EmptyString_NoSelectors()
    {
        var r = Make(); r.LoadCSSFromString("");
        Assert.Empty(r.Styles.Selectors);
    }

    [Fact] public void LoadCSSFromString_ValidBlock_ParsesSelector()
    {
        var r = Make();
        r.LoadCSSFromString(".foo { color: FF0000; font-size: 16; }");
        Assert.True(r.Styles.Selectors.ContainsKey(".foo"));
    }

    [Fact] public void LoadCSSFromString_BackgroundImage_PopulatesImageBackground()
    {
        var r = Make();
        r.LoadCSSFromString(".bar { background-image: img/bg.png; }");
        Assert.True(r.Styles.ImageBackgrounds.ContainsKey(".bar"));
    }

    [Fact] public void LoadCSSFromString_BackgroundSizeStretch_ParsedCorrectly()
    {
        var r = Make();
        r.LoadCSSFromString(".s { background-image: a.png; background-size: stretch; }");
        Assert.Equal(ScaleMode.Stretch, r.Styles.ImageBackgrounds[".s"].ScaleMode);
    }

    [Fact] public void LoadCSSFromString_BackgroundSizeNineSlice_ParsedCorrectly()
    {
        var r = Make();
        r.LoadCSSFromString(".s { background-image: a.png; background-size: nine-slice; }");
        Assert.Equal(ScaleMode.NineSlice, r.Styles.ImageBackgrounds[".s"].ScaleMode);
    }

    [Fact] public void LoadCSSFromString_BackgroundSlice_ParsesInsets()
    {
        var r = Make();
        r.LoadCSSFromString(".s { background-image: a.png; background-size: nine-slice; background-slice: 5 10 15 20; }");
        var ins = r.Styles.ImageBackgrounds[".s"].NineSlice!;
        Assert.Equal(5,  ins.Top);
        Assert.Equal(10, ins.Right);
        Assert.Equal(15, ins.Bottom);
        Assert.Equal(20, ins.Left);
    }

    [Fact] public void LoadCSSFromString_BackgroundOpacity_ParsedCorrectly()
    {
        var r = Make();
        r.LoadCSSFromString(".s { background-image: a.png; background-opacity: 0.5; }");
        Assert.Equal(0.5f, r.Styles.ImageBackgrounds[".s"].Opacity, precision: 3);
    }

    [Fact] public void LoadCSSFromString_BackgroundLayerBelow_SetsFlag()
    {
        var r = Make();
        r.LoadCSSFromString(".s { background-image: a.png; background-layer: below; }");
        Assert.False(r.Styles.ImageBackgrounds[".s"].DrawAboveColor);
    }

    [Fact] public void LoadCSSFromString_MissingBackgroundImage_NoImageBackground()
    {
        var r = Make();
        r.LoadCSSFromString(".s { color: FFFFFF; font-size: 20; }");
        Assert.Empty(r.Styles.ImageBackgrounds);
    }

    [Fact] public void LoadCSSFromString_ShowFalse_StylePresent()
    {
        var r = Make();
        r.LoadCSSFromString(".s { show: false; }");
        Assert.Equal("false", r.Styles.Selectors[".s"].Props["show"]);
    }

    [Fact] public void GetStyle_NonExistentSelector_ReturnsNull()
        => Assert.Null(Make().GetStyle(".nope"));

    [Fact] public void GetStyle_NonExistentProperty_ReturnsNull()
    {
        var r = Make();
        r.LoadCSSFromString(".s { color: FF0000; }");
        Assert.Null(r.GetStyleProperty(".s", "background-color"));
    }

    [Fact] public void ReloadCSS_RemovesOldSelector()
    {
        var r = Make();
        r.LoadCSSFromString(".old { color: FFFFFF; }");
        r.LoadCSSFromString(".new { color: FF0000; }");
        Assert.False(r.Styles.Selectors.ContainsKey(".old"));
        Assert.True(r.Styles.Selectors.ContainsKey(".new"));
    }
}
