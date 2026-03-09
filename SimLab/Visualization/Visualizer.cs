using SimLab.Simulator;
using System.Numerics;
using Raylib_cs;

namespace SimLab.Visualization;

// based on https://github.com/raylib-cs/raylib-cs/blob/master/Examples/Core/Camera3dFree.cs
internal class Visualizer {
    private const int ScreenWidth = 1200;
    private const int ScreenHeight = 800;
    public void Show(FrameBuffer frameBuffer) {
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "SimLab Visualization");
        Raylib.SetTargetFPS(60);

        Camera3D camera;
        camera.Position = new Vector3(10.0f, 10.0f, 10.0f);
        camera.Target = new Vector3(0.0f, 0.0f, 0.0f);
        camera.Up = new Vector3(0.0f, 1.0f, 0.0f);
        camera.FovY = 45.0f;
        camera.Projection = CameraProjection.Perspective;

        try {
            while (!Raylib.WindowShouldClose()) {
                Raylib.UpdateCamera(ref camera, CameraMode.Free);

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Raylib_cs.Color.Red);

                Raylib.BeginMode3D(camera);

                Raylib.DrawGrid(20, 1.0f);

                // draw cells from framebuffer
                Frame? frame = frameBuffer.GetFrame(0);
                if (frame != null) {
                    foreach (Position pos in frame.Cells) {
                        Raylib.DrawCube(new Vector3(pos.X, pos.Z, pos.Y), 0.5f, 0.5f, 0.5f, Raylib_cs.Color.Yellow);
                    }
                }

                Raylib.EndMode3D();
                Raylib.EndDrawing();
            }
        } finally {
            Raylib.CloseWindow();
        }
    }
}
