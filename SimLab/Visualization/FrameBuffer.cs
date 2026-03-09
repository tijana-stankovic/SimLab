using SimLab.Simulator;

namespace SimLab.Visualization;

internal class FrameBuffer {
    private readonly List<Frame> _frames = [];
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
}
