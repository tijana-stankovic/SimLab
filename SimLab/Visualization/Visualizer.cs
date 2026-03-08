using Raylib_cs;

namespace SimLab.Visualization;

internal class Visualizer {
    private const int ScreenWidth = 1200;
    private const int ScreenHeight = 800;

    public void Show(FrameBuffer frameBuffer) {
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "SimLab Visualization");
        Raylib.SetTargetFPS(60);

        try {
            while (!Raylib.WindowShouldClose()) {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Red);

                //TODO 
                // Drawing
                // Draw(frameBuffer.GetFrame(...));

                Raylib.EndDrawing();
            }
        } finally {
            Raylib.CloseWindow();
        }
    }
}
