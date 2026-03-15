using System.Numerics;
using Raylib_cs;

namespace SimLab.Visualization;

// based on examples:
// https://github.com/raysan5/raylib/tree/master/examples
// https://github.com/raylib-cs/raylib-cs/tree/master/Examples
// https://smarttis.mooo.com/raylib.cs
// https://github.com/grplyler/raylib-blender-camera
internal class Camera {
    public static void Update(ref Camera3D cam,
                              ref Vector3 target,
                              ref float distance,
                              ref float yaw,
                              ref float pitch,
                              float orbitSens = 0.005f, // the orbit sensitivity
                              float panSens   = 0.002f, // pan sensitivity (in relation to distance)
                              float zoomSens  = 1.0f) // zoom sensitivity
    {
        float dt = Raylib.GetFrameTime();

        // scroll = zoom
        float wheel = Raylib.GetMouseWheelMove();
        if (Math.Abs(wheel) > float.Epsilon)
            distance = MathF.Max(0.05f, distance * MathF.Pow(1.0f - 0.1f * zoomSens, wheel)); // log-scale zoom

        // mouse delta
        var md = Raylib.GetMouseDelta();

        // orbit (RMB)
        if (Raylib.IsMouseButtonDown(MouseButton.Left)) {
            float fast = Raylib.IsKeyDown(KeyboardKey.LeftControl) ? 10f : 1.0f;
            yaw   -= md.X * orbitSens * dt * 60f * fast;
            pitch -= md.Y * orbitSens * dt * 60f * fast;
            // limit pitch to not allow flipping over the top 
            float maxPitch = 89.0f * MathF.PI / 180f;
            pitch = Math.Clamp(pitch, -maxPitch, maxPitch);
        }

        // pan (MMB) – move the target point parallel to the camera plane, 
        // which effectively pans the camera around the scene.
        if (Raylib.IsMouseButtonDown(MouseButton.Right)) {
            // calculate the camera's local axes (forward, right, up) based on the current yaw and pitch angles
            Vector3 fwd, right, up;
            GetBasis(yaw, pitch, out fwd, out right, out up);
            // pan the target point based on the mouse movement, scaled by the distance and pan sensitivity
            float s = distance * panSens;
            target += (-right * md.X * s) + (up * md.Y * s);
        }

        // WASD/QE: local movement of the target, 
        // (allows the user to move the camera's focal point around the scene) 
        {
            Vector3 fwd, right, up;
            GetBasis(yaw, pitch, out fwd, out right, out up);
            Vector3 move = Vector3.Zero;
            float speed = distance * 0.75f * dt; // speed depends on distance

            if (Raylib.IsKeyDown(KeyboardKey.W)) move += fwd;
            if (Raylib.IsKeyDown(KeyboardKey.S)) move -= fwd;
            if (Raylib.IsKeyDown(KeyboardKey.A)) move -= right;
            if (Raylib.IsKeyDown(KeyboardKey.D)) move += right;
            if (Raylib.IsKeyDown(KeyboardKey.Q)) move -= up;
            if (Raylib.IsKeyDown(KeyboardKey.E)) move += up;

            if (move != Vector3.Zero)
                target += Vector3.Normalize(move) * speed;
        }

        // calculate the camera's position based on the target point, distance, yaw, and pitch
        cam.Target = target;
        cam.Position = target + SphericalToCartesian(distance, yaw, pitch);
        cam.Up = Vector3.UnitY;
        cam.FovY = MathF.Max(10f, MathF.Min(cam.FovY, 120f));
        cam.Projection = CameraProjection.Perspective;
    }

    // convert spherical coordinates (r, yaw, pitch) to Cartesian coordinates (x, y, z)
    static Vector3 SphericalToCartesian(float r, float yaw, float pitch) {
        // yaw: rotation around Y axis (azimuth), pitch: rotation around X axis (elevation)
        float cp = MathF.Cos(pitch);
        return new Vector3(
            r * MathF.Sin(-yaw) * cp,
            r * MathF.Sin(pitch),
            r * MathF.Cos(-yaw) * cp
        );
    }

    // calculate the camera's forward, right, and up vectors based on yaw and pitch angles
    static void GetBasis(float yaw, float pitch, out Vector3 fwd, out Vector3 right, out Vector3 up) {
        fwd = Vector3.Normalize(SphericalToCartesian(1f, yaw, pitch) * -1f);
        right = Vector3.Normalize(Vector3.Cross(fwd, Vector3.UnitY));
        up = Vector3.Normalize(Vector3.Cross(right, fwd));
    }
}
