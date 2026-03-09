using SimLab.Simulator;
using System.Numerics;
using Raylib_cs;

namespace SimLab.Visualization;

// based on https://github.com/raylib-cs/raylib-cs/blob/master/Examples/Core/Camera3dFree.cs
internal class Visualizer {
    private const int ScreenWidth = 1200;
    private const int ScreenHeight = 800;
    public int Show(FrameBuffer frameBuffer) {
        int currentFrameIndex = frameBuffer.GetStartFrameIndex();
        if (currentFrameIndex < 0) {
            return -1;
        }


        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "SimLab Visualization");
        Raylib.SetTargetFPS(60);

        Camera3D camera = new Camera3D(
            new Vector3(1, 30, 1), // position
            new Vector3(0, 0, 0),  // target
            new Vector3(0, 1, 0),  // up vector
            45,                    // fov
            CameraProjection.Perspective
        );

        try {
            while (!Raylib.WindowShouldClose()) {
                // handle left and right arrow keys to navigate through frames
                if (Raylib.IsKeyPressed(KeyboardKey.Left)) {
                    currentFrameIndex = Math.Max(currentFrameIndex - 1, 0);
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Right)) {
                    currentFrameIndex = Math.Min(currentFrameIndex + 1, frameBuffer.Count - 1);
                }                

                Raylib.UpdateCamera(ref camera, CameraMode.Free);

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Raylib_cs.Color.Red);

                Raylib.BeginMode3D(camera);

                Raylib.DrawGrid(20, 1.0f);

                // draw cells from framebuffer
                Frame? frame = frameBuffer.GetFrame(currentFrameIndex);
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

        frameBuffer.SetLastViewedFrameIndex(currentFrameIndex);
        return currentFrameIndex;
    }
}
