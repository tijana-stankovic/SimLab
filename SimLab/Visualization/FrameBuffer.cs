using SimLab.Simulator;

namespace SimLab.Visualization;

internal class FrameBuffer {
    private readonly List<Frame> _frames = [];
    public int Count => _frames.Count;
    public bool HasFrames => _frames.Count > 0;
    private int _lastViewedFrameIndex = -1;

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
}
