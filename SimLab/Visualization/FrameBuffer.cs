using SimLab.Simulator;
using System.Numerics;

namespace SimLab.Visualization;

internal class FrameBuffer {
    private readonly List<Frame> _frames = [];
    public int Count => _frames.Count;
    public bool HasFrames => _frames.Count > 0;
    private int _lastViewedFrameIndex = -1;
    private bool _hasSavedCameraState = false;

    // Default camera state used on first visualization call.
    private Vector3 _cameraTarget = Vector3.Zero;
    private float _cameraDistance = 50f; // target distance (how far you are from the target)
    private float _cameraYaw = -MathF.PI * 0.25f; // initial azimuth (-45 degrees)
    private float _cameraPitch = MathF.PI * 0.25f; // initial elevation (45 degrees)

    public int WorldSpace { get; }
    public int[] WorldDimensions { get; }

    public FrameBuffer(World world) {
        WorldSpace = world.Space;
        WorldDimensions = (int[])world.Dimensions.Clone();
    }

    public void Capture(Simulation simulation) {
        _frames.Add(Frame.CreateFromSimulation(simulation));
    }

    public Frame? GetFrame(int index) {
        if (index < 0 || index >= _frames.Count) {
            return null;
        }
        return _frames[index];
    }

    public int GetStartFrameIndex() {
        if (!HasFrames) {
            return -1;
        }

        if (_lastViewedFrameIndex < 0 || _lastViewedFrameIndex >= _frames.Count) {
            return _frames.Count - 1;
        }

        return _lastViewedFrameIndex;
    }

    public int GetLastViewedFrameIndex() {
        return _lastViewedFrameIndex;
    }

    public void SetLastViewedFrameIndex(int index) {
        if (!HasFrames) {
            _lastViewedFrameIndex = -1;
            return;
        }

        if (index < 0) {
            _lastViewedFrameIndex = 0;
        } else if (index > _frames.Count - 1) {
            _lastViewedFrameIndex = _frames.Count - 1;
        } else {
            _lastViewedFrameIndex = index;
        }
    }

    public void GetCameraState(out Vector3 target, out float distance, out float yaw, out float pitch) {
        target = _cameraTarget;
        distance = _cameraDistance;
        yaw = _cameraYaw;
        pitch = _cameraPitch;
    }

    public bool HasSavedCameraState() {
        return _hasSavedCameraState;
    }

    public void SetCameraState(Vector3 target, float distance, float yaw, float pitch) {
        _cameraTarget = target;
        _cameraDistance = distance;
        _cameraYaw = yaw;
        _cameraPitch = pitch;
        _hasSavedCameraState = true;
    }
}
