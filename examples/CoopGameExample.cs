using Raylib_cs;
using RaylibCoop;

// Minimal host example showing CoopFramework integration.
// Not compiled as part of the library — copy into a game project.

Raylib.InitWindow(1280, 720, "CoopFramework Example");
Raylib.SetTargetFPS(60);

var inputMgr = new InputManager(maxPlayers: 4);
inputMgr.Initialize();
inputMgr.LoadOverlayCSS("examples/overlay.css");

inputMgr.OnControllerConnected    += id => Console.WriteLine($"Controller {id} connected");
inputMgr.OnControllerDisconnected += id => Console.WriteLine($"Controller {id} disconnected");
inputMgr.KeyboardFallback.OnEscapePressed += () => Console.WriteLine("ESC pressed");

while (!Raylib.WindowShouldClose())
{
    var input = inputMgr.ReadControllers();

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);

    int y = 20;
    foreach (var (id, state) in input)
    {
        string label = state.IsKeyboard ? "KB" : $"P{id + 1}";
        float lx = state.Axes["left_stick_x"], ly = state.Axes["left_stick_y"];
        Raylib.DrawText($"{label}  LX:{lx:F2} LY:{ly:F2}  A:{state.Buttons["a"]}  B:{state.Buttons["b"]}", 20, y, 20, Color.Green);
        y += 24;
    }

    inputMgr.RenderOverlays();
    Raylib.EndDrawing();
}

inputMgr.Shutdown();
Raylib.CloseWindow();
