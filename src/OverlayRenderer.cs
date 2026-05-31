using System.Text.RegularExpressions;

namespace RaylibCoop;

// CSS properties supported:
//   position: x, y          — screen position in pixels
//   size: w, h              — element size in pixels
//   color: RRGGBB           — text color (hex)
//   background-color: RRGGBB
//   font-size: N
//   show: true|false
//   background-image: path/to/file.png
//   background-size: stretch|contain|cover|tile|nine-slice
//   background-align-h: left|center|right
//   background-align-v: top|center|bottom
//   background-slice: T R B L  (or single value for all sides)
//   background-tint: RRGGBB
//   background-opacity: 0.0–1.0
//   background-layer: above|below

public class StyleBlock
{
    public Dictionary<string, string> Props { get; } = new();
}

public enum ScaleMode { Stretch, Contain, Cover, Tile, NineSlice }
public enum AlignH    { Left, Center, Right }
public enum AlignV    { Top, Center, Bottom }

public class NineSliceInsets
{
    public int Top, Right, Bottom, Left;
}

public class ImageBackground
{
    public string ImagePath   = "";
    public ScaleMode ScaleMode = ScaleMode.Stretch;
    public AlignH AlignH       = AlignH.Center;
    public AlignV AlignV       = AlignV.Center;
    public NineSliceInsets? NineSlice;
    public string Tint         = "FFFFFF";
    public float Opacity       = 1f;
    public bool DrawAboveColor = true;
}

public class OverlayStyles
{
    public Dictionary<string, StyleBlock>    Selectors        { get; } = new();
    public Dictionary<string, ImageBackground> ImageBackgrounds { get; } = new();
}

public class TextureCache
{
    private readonly Dictionary<string, int> _cache = new();

    public int Load(string filePath)
    {
        if (_cache.TryGetValue(filePath, out int existing)) return existing;
        var tex = Raylib_cs.Raylib.LoadTexture(filePath);
        if (tex.Id == 0)
        {
            Console.Error.WriteLine($"[TextureCache] Failed to load: {filePath}");
            return -1;
        }
        _cache[filePath] = (int)tex.Id;
        return (int)tex.Id;
    }

    public void Unload(string filePath)
    {
        if (_cache.TryGetValue(filePath, out int id))
        {
            Raylib_cs.Raylib.UnloadTexture(new Raylib_cs.Texture2D { Id = (uint)id });
            _cache.Remove(filePath);
        }
    }

    public void UnloadAll()
    {
        foreach (var (_, id) in _cache)
            Raylib_cs.Raylib.UnloadTexture(new Raylib_cs.Texture2D { Id = (uint)id });
        _cache.Clear();
    }
}

public class OverlayRenderer
{
    public OverlayStyles Styles { get; } = new();
    private readonly TextureCache _textures = new();

    private float GetDeltaTime()    => Raylib_cs.Raylib.GetFrameTime();
    private int   GetScreenWidth()  => Raylib_cs.Raylib.GetScreenWidth();
    private int   GetScreenHeight() => Raylib_cs.Raylib.GetScreenHeight();

    // ── CSS loading ────────────────────────────────────────────────────────

    public void LoadCSS(string path)
    {
        if (!File.Exists(path)) { Console.Error.WriteLine($"[OverlayRenderer] CSS not found: {path}"); return; }
        LoadCSSFromString(File.ReadAllText(path));
    }

    public void LoadCSSFromString(string css)
    {
        // Clear previous
        Styles.Selectors.Clear();
        Styles.ImageBackgrounds.Clear();

        var blockRx = new Regex(@"([\.\#\w][\w\-]*)\s*\{([^}]*)\}", RegexOptions.Singleline);
        foreach (Match m in blockRx.Matches(css))
        {
            string selector = m.Groups[1].Value.Trim();
            string body     = m.Groups[2].Value;

            var block = new StyleBlock();
            foreach (Match prop in Regex.Matches(body, @"([\w\-]+)\s*:\s*([^;]+);"))
            {
                string k = prop.Groups[1].Value.Trim();
                string v = prop.Groups[2].Value.Trim();
                block.Props[k] = v;
            }
            Styles.Selectors[selector] = block;

            var bg = ImageBackgroundParser.Parse(block.Props);
            if (bg != null)
            {
                Styles.ImageBackgrounds[selector] = bg;
                if (!string.IsNullOrEmpty(bg.ImagePath))
                    _textures.Load(bg.ImagePath);
            }
        }
    }

    public void ReloadCSS(string path)
    {
        _textures.UnloadAll();
        LoadCSS(path);
    }

    public StyleBlock? GetStyle(string selector)
        => Styles.Selectors.TryGetValue(selector, out var b) ? b : null;

    public string? GetStyleProperty(string selector, string property)
        => GetStyle(selector)?.Props.TryGetValue(property, out var v) == true ? v : null;

    // ── Rendering ──────────────────────────────────────────────────────────

    public void RenderAll(IReadOnlyList<ControllerState> states)
    {
        foreach (var (selector, block) in Styles.Selectors)
        {
            if (block.Props.TryGetValue("show", out string? show) && show == "false")
                continue;
            RenderElement(selector, block, states);
        }
    }

    private void RenderElement(string selector, StyleBlock block, IReadOnlyList<ControllerState> states)
    {
        if (!block.Props.TryGetValue("position", out string? posStr)) return;
        if (!block.Props.TryGetValue("size",     out string? sizeStr)) return;

        var pos  = ParsePair(posStr);
        var size = ParsePair(sizeStr);
        int x = (int)pos.X, y = (int)pos.Y, w = (int)size.X, h = (int)size.Y;

        // Per-player horizontal offset
        IEnumerable<ControllerState?> stateLoop = states.Count > 0
            ? states.Cast<ControllerState?>()
            : new ControllerState?[] { null };
        foreach (var state in stateLoop)
        {
            int px = x;
            if (state != null && state.PlayerId >= 0)
                px += state.PlayerId * (w + 8);

            // Background color
            if (block.Props.TryGetValue("background-color", out string? bgHex))
            {
                var bgColor = ParseRaylibColor(bgHex, 1f);
                Raylib_cs.Raylib.DrawRectangle(px, y, w, h, bgColor);
            }

            // Image background (below text)
            if (Styles.ImageBackgrounds.TryGetValue(selector, out var imgBg) && !imgBg.DrawAboveColor)
                DrawImageBackground(imgBg, px, y, w, h);

            // Text label
            if (block.Props.TryGetValue("color", out string? colorHex))
            {
                string label = state != null ? $"P{state.PlayerId + 1}" : "";
                if (block.Props.TryGetValue("font-size", out string? fsStr) && int.TryParse(fsStr, out int fs))
                {
                    var textColor = ParseRaylibColor(colorHex, 1f);
                    Raylib_cs.Raylib.DrawText(label, px + 4, y + 4, fs, textColor);
                }
            }

            // Image background (above text)
            if (Styles.ImageBackgrounds.TryGetValue(selector, out imgBg) && imgBg.DrawAboveColor)
                DrawImageBackground(imgBg, px, y, w, h);
        }
    }

    private void DrawImageBackground(ImageBackground bg, int x, int y, int w, int h)
    {
        if (bg.Opacity <= 0f || string.IsNullOrEmpty(bg.ImagePath)) return;
        int handle = _textures.Load(bg.ImagePath);
        if (handle < 0) return;
        var tex = ResolveTexture(handle);
        if (tex.Width == 0) return;

        switch (bg.ScaleMode)
        {
            case ScaleMode.Stretch:    DrawImageStretch(tex, bg, x, y, w, h);   break;
            case ScaleMode.Contain:    DrawImageContain(tex, bg, x, y, w, h);   break;
            case ScaleMode.Cover:      DrawImageCover(tex, bg, x, y, w, h);     break;
            case ScaleMode.Tile:       DrawImageTile(tex, bg, x, y, w, h);      break;
            case ScaleMode.NineSlice:  DrawImageNineSlice(tex, bg, x, y, w, h); break;
        }
    }

    private void DrawImageStretch(Raylib_cs.Texture2D tex, ImageBackground bg, int x, int y, int w, int h)
    {
        var src  = new Raylib_cs.Rectangle(0, 0, tex.Width, tex.Height);
        var dest = new Raylib_cs.Rectangle(x, y, w, h);
        var tint = ParseRaylibColor(bg.Tint, bg.Opacity);
        Raylib_cs.Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private void DrawImageContain(Raylib_cs.Texture2D tex, ImageBackground bg, int x, int y, int w, int h)
    {
        float scale = Math.Min((float)w / tex.Width, (float)h / tex.Height);
        int dw = (int)(tex.Width * scale), dh = (int)(tex.Height * scale);
        int dx = AlignOffset(bg.AlignH, x, w, dw);
        int dy = AlignOffset(bg.AlignV, y, h, dh);
        var src  = new Raylib_cs.Rectangle(0, 0, tex.Width, tex.Height);
        var dest = new Raylib_cs.Rectangle(dx, dy, dw, dh);
        var tint = ParseRaylibColor(bg.Tint, bg.Opacity);
        Raylib_cs.Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private void DrawImageCover(Raylib_cs.Texture2D tex, ImageBackground bg, int x, int y, int w, int h)
    {
        float scale = Math.Max((float)w / tex.Width, (float)h / tex.Height);
        int dw = (int)(tex.Width * scale), dh = (int)(tex.Height * scale);
        int dx = AlignOffset(bg.AlignH, x, w, dw);
        int dy = AlignOffset(bg.AlignV, y, h, dh);
        var src  = new Raylib_cs.Rectangle(0, 0, tex.Width, tex.Height);
        var dest = new Raylib_cs.Rectangle(dx, dy, dw, dh);
        var tint = ParseRaylibColor(bg.Tint, bg.Opacity);
        Raylib_cs.Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private void DrawImageTile(Raylib_cs.Texture2D tex, ImageBackground bg, int x, int y, int w, int h)
    {
        var tint = ParseRaylibColor(bg.Tint, bg.Opacity);
        Raylib_cs.Raylib.BeginScissorMode(x, y, w, h);
        for (int ty = y; ty < y + h; ty += tex.Height)
        for (int tx = x; tx < x + w; tx += tex.Width)
            Raylib_cs.Raylib.DrawTexture(tex, tx, ty, tint);
        Raylib_cs.Raylib.EndScissorMode();
    }

    private void DrawImageNineSlice(Raylib_cs.Texture2D tex, ImageBackground bg, int x, int y, int w, int h)
    {
        var ins  = bg.NineSlice ?? new NineSliceInsets { Top = 0, Right = 0, Bottom = 0, Left = 0 };
        var tint = ParseRaylibColor(bg.Tint, bg.Opacity);

        // Source corners/edges in texture space
        int tw = tex.Width, th = tex.Height;
        var src = new Raylib_cs.NPatchInfo
        {
            Source = new Raylib_cs.Rectangle(0, 0, tw, th),
            Left   = ins.Left,
            Top    = ins.Top,
            Right  = ins.Right,
            Bottom = ins.Bottom,
            Layout = Raylib_cs.NPatchLayout.NinePatch,
        };
        var dest = new Raylib_cs.Rectangle(x, y, w, h);
        Raylib_cs.Raylib.DrawTextureNPatch(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private Raylib_cs.Texture2D ResolveTexture(int handle)
        => new Raylib_cs.Texture2D { Id = (uint)handle };

    private static Raylib_cs.Color ParseRaylibColor(string hex, float opacity)
    {
        hex = hex.TrimStart('#');
        byte r = 0, g = 0, b = 0;
        if (hex.Length >= 6)
        {
            r = Convert.ToByte(hex[0..2], 16);
            g = Convert.ToByte(hex[2..4], 16);
            b = Convert.ToByte(hex[4..6], 16);
        }
        byte a = (byte)(Math.Clamp(opacity, 0f, 1f) * 255f);
        return new Raylib_cs.Color(r, g, b, a);
    }

    private static System.Numerics.Vector2 ParsePair(string s)
    {
        var parts = s.Split(',');
        float x = parts.Length > 0 && float.TryParse(parts[0].Trim(), out float px) ? px : 0f;
        float y = parts.Length > 1 && float.TryParse(parts[1].Trim(), out float py) ? py : 0f;
        return new System.Numerics.Vector2(x, y);
    }

    private static int AlignOffset(AlignH align, int containerX, int containerW, int itemW) => align switch
    {
        AlignH.Center => containerX + (containerW - itemW) / 2,
        AlignH.Right  => containerX + containerW - itemW,
        _             => containerX,
    };

    private static int AlignOffset(AlignV align, int containerY, int containerH, int itemH) => align switch
    {
        AlignV.Center => containerY + (containerH - itemH) / 2,
        AlignV.Bottom => containerY + containerH - itemH,
        _             => containerY,
    };

    public void UnloadAll() => _textures.UnloadAll();
}

public static class ImageBackgroundParser
{
    public static ImageBackground? Parse(Dictionary<string, string> props)
    {
        if (!props.TryGetValue("background-image", out string? imgPath)
            || string.IsNullOrWhiteSpace(imgPath))
            return null;

        var bg = new ImageBackground { ImagePath = imgPath };

        if (props.TryGetValue("background-size", out string? size))
            bg.ScaleMode = size.ToLower() switch
            {
                "contain"    => ScaleMode.Contain,
                "cover"      => ScaleMode.Cover,
                "tile"       => ScaleMode.Tile,
                "nine-slice" => ScaleMode.NineSlice,
                _            => ScaleMode.Stretch,
            };

        if (props.TryGetValue("background-align-h", out string? alignH))
            bg.AlignH = alignH.ToLower() switch
            {
                "center" => AlignH.Center,
                "right"  => AlignH.Right,
                _        => AlignH.Left,
            };

        if (props.TryGetValue("background-align-v", out string? alignV))
            bg.AlignV = alignV.ToLower() switch
            {
                "center" => AlignV.Center,
                "bottom" => AlignV.Bottom,
                _        => AlignV.Top,
            };

        if (props.TryGetValue("background-slice", out string? sliceStr))
            bg.NineSlice = ParseInsets(sliceStr);

        if (props.TryGetValue("background-tint", out string? tint))
            bg.Tint = tint.TrimStart('#');

        if (props.TryGetValue("background-opacity", out string? opacityStr)
            && float.TryParse(opacityStr, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out float opacity))
            bg.Opacity = Math.Clamp(opacity, 0f, 1f);

        if (props.TryGetValue("background-layer", out string? layer))
            bg.DrawAboveColor = layer.ToLower() != "below";

        return bg;
    }

    private static NineSliceInsets ParseInsets(string s)
    {
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int[] vals = parts.Select(p => int.TryParse(p, out int v) ? v : 0).ToArray();
        return vals.Length switch
        {
            1 => new NineSliceInsets { Top = vals[0], Right = vals[0], Bottom = vals[0], Left = vals[0] },
            2 => new NineSliceInsets { Top = vals[0], Right = vals[1], Bottom = vals[0], Left = vals[1] },
            4 => new NineSliceInsets { Top = vals[0], Right = vals[1], Bottom = vals[2], Left = vals[3] },
            _ => new NineSliceInsets { Top = 0, Right = 0, Bottom = 0, Left = 0 },
        };
    }
}
