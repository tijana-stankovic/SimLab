using SimLab.Simulator;

namespace SimLab.Visualization;

internal class Frame {
    public long Cycle { get; }
    public IReadOnlyList<Cell> Cells { get; }

    public Frame(long cycle, List<Cell> cells) {
        Cycle = cycle;
        Cells = cells;
    }
}
