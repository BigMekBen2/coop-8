namespace RaylibCoop;

public class JoinPopupRenderer
{
    private readonly int _screenW;
    private readonly int _screenH;

    // Scale with screen — at 3840×2160 these yield ~768×720 popups with ~48px gaps
    private int PopupW   => _screenW / 5;
    private int PopupH   => _screenH / 3;
    private int GridGap  => Math.Max(_screenW / 80, 8);
    private int GridPadY => _screenH / 20;

    public JoinPopupRenderer(int screenWidth, int screenHeight)
    {
        _screenW = screenWidth;
        _screenH = screenHeight;
    }

    public void Render(List<JoinSlot> slots, bool accepting)
    {
        if (accepting)
            RenderJoinPrompt();

        foreach (var slot in slots)
            RenderPopup(slot);
    }

    // ── Join prompt ───────────────────────────────────────────────────────

    private void RenderJoinPrompt()
    {
        int fs = (int)(_screenH * 0.038f);
        string text = "PRESS BUTTON ON YOUR GAMEPAD TO JOIN";

        bool blink = (int)(Raylib_cs.Raylib.GetTime() * 2) % 2 == 0;
        if (!blink) return;

        int tw = Raylib_cs.Raylib.MeasureText(text, fs);
        int x  = (_screenW - tw) / 2;
        int y  = _screenH - (int)(_screenH * 0.12f);
        Raylib_cs.Raylib.DrawText(text, x, y, fs, PlayerPalette.CrtGreen.ToRaylib());
    }

    // ── Pop-up ────────────────────────────────────────────────────────────

    private void RenderPopup(JoinSlot slot)
    {
        float scale = EaseOutBack(slot.AppearProgress);
        if (scale <= 0.01f) return;

        var (cx, cy) = GridCenter(slot.GridCol, slot.GridRow);
        int pw = (int)(PopupW * scale);
        int ph = (int)(PopupH * scale);
        int px = cx - pw / 2;
        int py = cy - ph / 2;

        var color = PlayerPalette.Colors[slot.PlayerId % PlayerPalette.Colors.Length].ToRaylib();
        byte alpha = (byte)(255 * Math.Clamp(slot.AppearProgress, 0f, 1f));
        var panelBg   = new Raylib_cs.Color((byte)20, (byte)20, (byte)30, alpha);
        var borderCol = new Raylib_cs.Color(color.R, color.G, color.B, alpha);
        var dimGreen  = new Raylib_cs.Color(PlayerPalette.CrtGreen.R, PlayerPalette.CrtGreen.G,
                                            PlayerPalette.CrtGreen.B, alpha);

        // Panel background
        Raylib_cs.Raylib.DrawRectangle(px, py, pw, ph, panelBg);
        // Border
        Raylib_cs.Raylib.DrawRectangleLinesEx(new Raylib_cs.Rectangle(px, py, pw, ph), 3f, borderCol);

        // Player tag
        int tagFs = (int)(ph * 0.18f);
        string tag = $"P{slot.PlayerId + 1}";
        int tagW = Raylib_cs.Raylib.MeasureText(tag, tagFs);
        Raylib_cs.Raylib.DrawText(tag, cx - tagW / 2, py + 8, tagFs, borderCol);

        // Separator
        int sepY = py + tagFs + 16;
        Raylib_cs.Raylib.DrawLine(px + 8, sepY, px + pw - 8, sepY, dimGreen);

        // Char slots
        DrawCharSlots(slot, px, py, pw, ph, sepY, color, alpha);

        // Control hints
        DrawControlHints(slot, px, py, pw, ph, alpha, dimGreen);
    }

    private void DrawCharSlots(JoinSlot slot, int px, int py, int pw, int ph,
                                int sepY, Raylib_cs.Color playerColor, byte alpha)
    {
        int slotCount = 3;
        int slotW = (int)(pw * 0.22f);
        int slotH = (int)(ph * 0.36f);
        int totalW = slotCount * slotW + (slotCount - 1) * 8;
        int startX = px + (pw - totalW) / 2;
        int slotY  = sepY + 12;
        int mainFs = (int)(slotH * 0.58f);
        int adjFs  = (int)(mainFs * 0.55f);

        double time = Raylib_cs.Raylib.GetTime();

        for (int i = 0; i < slotCount; i++)
        {
            int sx = startX + i * (slotW + 8);
            bool isCursor = i == slot.Entry.Cursor;

            // Slot background
            Raylib_cs.Raylib.DrawRectangle(sx, slotY, slotW, slotH,
                new Raylib_cs.Color((byte)10, (byte)10, (byte)20, alpha));

            // Border
            if (isCursor)
                Raylib_cs.Raylib.DrawRectangleLinesEx(
                    new Raylib_cs.Rectangle(sx, slotY, slotW, slotH), 3f,
                    new Raylib_cs.Color(playerColor.R, playerColor.G, playerColor.B, alpha));
            else
                Raylib_cs.Raylib.DrawRectangleLinesEx(
                    new Raylib_cs.Rectangle(sx, slotY, slotW, slotH), 1f,
                    new Raylib_cs.Color((byte)40, (byte)80, (byte)40, alpha));

            // Adjacent chars (cursor column only)
            if (isCursor)
            {
                int wheel = InitialsEntry.Wheel.Length;
                int aboveIdx = ((slot.Entry.WheelIndices[i] - 1) + wheel) % wheel;
                int belowIdx = (slot.Entry.WheelIndices[i] + 1) % wheel;
                string aboveStr = CharLabel(InitialsEntry.Wheel[aboveIdx]);
                string belowStr = CharLabel(InitialsEntry.Wheel[belowIdx]);

                var adjColor = new Raylib_cs.Color((byte)80, (byte)120, (byte)80, alpha);
                int aboveW = Raylib_cs.Raylib.MeasureText(aboveStr, adjFs);
                int belowW = Raylib_cs.Raylib.MeasureText(belowStr, adjFs);
                Raylib_cs.Raylib.DrawText(aboveStr, sx + (slotW - aboveW) / 2, slotY - adjFs - 2, adjFs, adjColor);
                Raylib_cs.Raylib.DrawText(belowStr, sx + (slotW - belowW) / 2, slotY + slotH + 2,  adjFs, adjColor);
            }

            // Current char
            string label = CharLabel(InitialsEntry.Wheel[slot.Entry.WheelIndices[i]]);
            int lw = Raylib_cs.Raylib.MeasureText(label, mainFs);
            int lx = sx + (slotW - lw) / 2;
            int ly = slotY + (slotH - mainFs) / 2;
            var charColor = new Raylib_cs.Color(PlayerPalette.CrtGreen.R, PlayerPalette.CrtGreen.G,
                                                PlayerPalette.CrtGreen.B, alpha);
            Raylib_cs.Raylib.DrawText(label, lx, ly, mainFs, charColor);

            // Cursor blink underline (4Hz)
            if (isCursor && (int)(time * 8) % 2 == 0)
            {
                int barY = slotY + slotH - 6;
                Raylib_cs.Raylib.DrawRectangle(sx + 4, barY, slotW - 8, 4,
                    new Raylib_cs.Color(playerColor.R, playerColor.G, playerColor.B, alpha));
            }
        }
    }

    private void DrawControlHints(JoinSlot slot, int px, int py, int pw, int ph,
                                  byte alpha, Raylib_cs.Color dimGreen)
    {
        int fs   = (int)(ph * 0.065f);
        int hintY1 = py + ph - fs * 2 - 16;
        int hintY2 = hintY1 + fs + 4;

        string row1 = slot.IsKeyboard ? "UP/DOWN SELECT" : "DPad UP/DOWN SELECT";
        string ok   = slot.IsKeyboard ? "ENTER/RIGHT OK"  : "A/RIGHT OK";
        string bk   = slot.IsKeyboard ? "BKSP BACK"       : "B BACK";

        // Row 1 centered
        int r1w = Raylib_cs.Raylib.MeasureText(row1, fs);
        Raylib_cs.Raylib.DrawText(row1, px + (pw - r1w) / 2, hintY1, fs, dimGreen);

        // Row 2: ok left, bk right
        Raylib_cs.Raylib.DrawText(ok, px + 8, hintY2, fs, dimGreen);
        int bkw = Raylib_cs.Raylib.MeasureText(bk, fs);
        Raylib_cs.Raylib.DrawText(bk, px + pw - bkw - 8, hintY2, fs, dimGreen);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private (int cx, int cy) GridCenter(int col, int row)
    {
        int pw = PopupW, ph = PopupH, gap = GridGap;
        int totalW = 4 * pw + 3 * gap;
        int originX = (_screenW - totalW) / 2;
        int originY = GridPadY;

        int cx = originX + col * (pw + gap) + pw / 2;
        int cy = originY + row * (ph + gap) + ph / 2;
        return (cx, cy);
    }

    private static string CharLabel(char c) => c switch
    {
        ' ' => "SPC",
        _   => c.ToString(),
    };

    // Ease-out back (overshoot spring)
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float tm = t - 1f;
        return 1f + c3 * tm * tm * tm + c1 * tm * tm;
    }
}
