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
        //TODO
        // add frame to frame buffer
        // e.g.
        // _frames.Add(new Frame(simulation.Cycle, simulation.GetAllCells().Select(ch => ch.Cell.Clone()).ToList()));
    }

    public Frame GetFrame(int index) {
        return _frames[index];
    }
}
