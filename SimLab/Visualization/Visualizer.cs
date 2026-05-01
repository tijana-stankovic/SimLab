using SimLab.Simulator;
using System.Numerics;
using Raylib_cs;
using RayColor = Raylib_cs.Color;

namespace SimLab.Visualization;

// based on:
// https://github.com/raylib-cs/raylib-cs/blob/master/Examples/Core/Camera3dFree.cs
// https://smarttis.mooo.com/raylib.cs
internal class Visualizer {
    private const int ScreenWidth = 1200;
    private const int ScreenHeight = 800;
    private const float CellSize = 1.0f;

    private static bool DisplayHUD { get; set; } = true;

    public static int Show(FrameBuffer frameBuffer) {
        int currentFrameIndex = frameBuffer.GetStartFrameIndex();
        if (currentFrameIndex < 0) {
            return -1;
        }
        Frame? currentFrame = frameBuffer.GetFrame(currentFrameIndex);
        if (currentFrame == null) {
            return -1;
        }

        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "SimLab Visualization");
        Raylib.SetTargetFPS(60);

        // custom camera settings
        Vector3 target;
        float distance;
        float yaw;
        float pitch;

        // load camera state from frame buffer:
        // first SHOW call -> default values
        // next SHOW calls -> last saved camera position
        frameBuffer.GetCameraState(out target, out distance, out yaw, out pitch);

        // this initial camera position is not really important, 
        // because the camera will be updated in the first frame of the visualization loop
        // based on the target, distance, yaw, and pitch settings above
        Camera3D camera = new Camera3D(
            new Vector3(10, 10, 10),        // position (camera is located at this point)
            target,                         // target (camera looks at this point)
            new Vector3(0, 1, 0),           // up vector (used to determine the camera's orientation)
            45,                             // fov (degrees, defines the field of view)
            CameraProjection.Perspective    // projection mode (perspective or orthographic)
        );

        try {
            while (!Raylib.WindowShouldClose()) {
                UpdateCurrentFrameIndex(frameBuffer, ref currentFrameIndex);

                // RayLib camera update:
                // Raylib.UpdateCamera(ref camera, CameraMode.Free);

                // Custom camera update
                Camera.Update(ref camera, ref target, ref distance, ref yaw, ref pitch,
                        // camera movement sensitivity settings:
                        orbitSens: 0.005f, 
                        panSens: 0.002f, 
                        zoomSens: 1.0f);

                DrawCurrentFrame(frameBuffer, currentFrameIndex, camera);
            }
        } finally {
            frameBuffer.SetCameraState(target, distance, yaw, pitch);
            Raylib.CloseWindow();
        }

        frameBuffer.SetLastViewedFrameIndex(currentFrameIndex);
        return currentFrameIndex;
    }

    // handles user input to navigate through frames using Left and Right arrow keys, 
    // with optional Shift key for faster navigation
    private static void UpdateCurrentFrameIndex(FrameBuffer frameBuffer, ref int currentFrameIndex) {
        // handle left and right arrow keys to navigate through frames
        if (Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift)) {
            if (Raylib.IsKeyDown(KeyboardKey.Left)) {
                currentFrameIndex = Math.Max(currentFrameIndex - 1, 0);
            }
            if (Raylib.IsKeyDown(KeyboardKey.Right)) {
                currentFrameIndex = Math.Min(currentFrameIndex + 1, frameBuffer.Count - 1);
            }
        } else {
            if (Raylib.IsKeyPressed(KeyboardKey.Left)) {
                currentFrameIndex = Math.Max(currentFrameIndex - 1, 0);
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Right)) {
                currentFrameIndex = Math.Min(currentFrameIndex + 1, frameBuffer.Count - 1);
            }
        }
    }

    // draws the current frame, including the grid and cells, and displays HUD information
    private static void DrawCurrentFrame(FrameBuffer frameBuffer, int currentFrameIndex, Camera3D camera) {
        Frame frame = frameBuffer.GetFrame(currentFrameIndex)!;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new RayColor(
            frame.BackgroundColor.R,
            frame.BackgroundColor.G,
            frame.BackgroundColor.B,
            (byte)255));

        Raylib.BeginMode3D(camera);
        DrawGrid(frameBuffer, frame);
        DrawCells(frameBuffer.WorldSpace, frame);
        Raylib.EndMode3D();

        DrawHUD(frameBuffer, currentFrameIndex, frame);

        Raylib.EndDrawing();
    }

    // dinamically calculates grid bounds based on world dimensions and cell positions in the frame, 
    // then draws the grid lines
    private static void DrawGrid(FrameBuffer frameBuffer, Frame frame) {
        // first, calculates grid bounds based on frame buffer world dimensions and cell positions
        // then, draws grid lines and coordinate axes

        // when any world dimension are not specified, use default value of 10
        int worldWidth = frameBuffer.WorldDimensions.Length > 0 ? frameBuffer.WorldDimensions[0] : 10;
        int worldDepth;
        if (frameBuffer.WorldSpace == 2) {
            // in 2D world, we use the second dimension for worldDepth
            worldDepth = frameBuffer.WorldDimensions.Length > 1 ? frameBuffer.WorldDimensions[1] : 10;
        } else {
            // in 3D world, we use the third dimension for worldDepth
            worldDepth = frameBuffer.WorldDimensions.Length > 2 ? frameBuffer.WorldDimensions[2] : 10;
        }

        // initial values for grid bounds
        // then it will be adjusted based on cell positions in the frame
        int minX = 0;
        int minZ = 0;
        int maxX = Math.Max(worldWidth, 10); // 10 is the minimum grid size
        int maxZ = Math.Max(worldDepth, 10); // 10 is the minimum grid size

        foreach (FrameCell cell in frame.Cells) {
            Position pos = cell.Position;

            minX = Math.Min(minX, pos.X);
            maxX = Math.Max(maxX, pos.X + 1);

            if (frameBuffer.WorldSpace == 2) {
                // in 2D world, we use the second dimension 
                minZ = Math.Min(minZ, pos.Y);
                maxZ = Math.Max(maxZ, pos.Y + 1);               
            } else {
                // in 3D world, we use the third dimension 
                minZ = Math.Min(minZ, pos.Z);
                maxZ = Math.Max(maxZ, pos.Z + 1);                               
            }
        }

        // draw grid lines
        RayColor lineColor = RayColor.DarkGray; // grid line color

        for (int x = minX; x <= maxX; x++) {
            Raylib.DrawLine3D(
                new Vector3(x, 0f, minZ),
                new Vector3(x, 0f, maxZ),
                lineColor);
        }

        for (int z = minZ; z <= maxZ; z++) {
            Raylib.DrawLine3D(
                new Vector3(minX, 0f, z),
                new Vector3(maxX, 0f, z),
                lineColor);
        }

        // draw coordinate axes
        Raylib.DrawLine3D(new Vector3(0, 0, 0), new Vector3(maxX, 0, 0), RayColor.Red);   // X axis
        if (frameBuffer.WorldSpace == 3) {
            Raylib.DrawLine3D(new Vector3(0, 0, 0), new Vector3(0, 10f, 0), RayColor.Green); // Y axis
        }
        Raylib.DrawLine3D(new Vector3(0, 0, 0), new Vector3(0, 0, maxZ), RayColor.Blue);  // Z axis
    }

    // draws cells as cubes based on their positions in the frame
    private static void DrawCells(int worldSpace, Frame frame) {
        foreach (FrameCell cell in frame.Cells) {
            Vector3 cellPosition = AdjustedCellPosition(worldSpace, cell.Position);
            RayColor fillColor = new RayColor(cell.Color.R, cell.Color.G, cell.Color.B, (byte)255);

            Raylib.DrawCube(cellPosition, CellSize, CellSize, CellSize, fillColor);
            Raylib.DrawCubeWires(cellPosition, CellSize, CellSize, CellSize, new RayColor(20, 80, 20, 255));
        }
    }

    // Adjusts the position of a cell based on the world space (2D or 3D).
    // In 2D world, the Y coordinate is used for depth (Z axis in 3D), 
    // while in 3D world, the Z coordinate is used for depth.
    // Also, the position is adjusted to be aligned with the grid lines.
    private static Vector3 AdjustedCellPosition(int worldSpace, Position position) {
        if (worldSpace == 2) {
            return new Vector3(position.X + CellSize/2, CellSize/2, position.Y + CellSize/2);
        }

        return new Vector3(position.X + CellSize/2, position.Y + CellSize/2, position.Z + CellSize/2);
    }

    // draws HUD information
    private static void DrawHUD(FrameBuffer frameBuffer, int currentFrameIndex, Frame frame) {
        // HUD control: toggle display of HUD information with H key
        if (Raylib.IsKeyPressed(KeyboardKey.H)) {
            DisplayHUD = !DisplayHUD;
        }
        // HUD information
        if (DisplayHUD) {
            if (currentFrameIndex == 0) {
                Raylib.DrawText($"Initial position. Total simulation cycles: {frameBuffer.Count-1}", 10, 10, 20, RayColor.White);
            } else {
                Raylib.DrawText($"Cycle: {currentFrameIndex}/{frameBuffer.Count-1}", 10, 10, 20, RayColor.White);
            }
            if (frame != null)
                Raylib.DrawText($"Cells: {frame.Cells.Count}", 10, 40, 20, RayColor.White);
            Raylib.DrawText("[LeftMouseButton]=orbit  [RightMouseButton or QEAD]=pan [Wheel or WS]=zoom", 10, 70, 20, RayColor.LightGray);
            Raylib.DrawText("LEFT/RIGHT = frame navigation (with Shift for faster navigation)", 10, 100, 20, RayColor.LightGray);
            Raylib.DrawText("ESC = close visualization", 10, 130, 20, RayColor.LightGray);
            Raylib.DrawText("H = HUD display on/off", 10, 160, 20, RayColor.LightGray);
        }
    }
}
