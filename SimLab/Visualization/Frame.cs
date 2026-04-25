using SimLab.Simulator;

namespace SimLab.Visualization;

internal class Frame {
    public long Cycle { get; }
    public IReadOnlyList<FrameCell> Cells { get; }

    public Frame(long cycle, List<FrameCell> cells) {
        Cycle = cycle;
        Cells = cells;
    }

    public static Frame CreateFromSimulation(Simulation simulation) {
        List<FrameCell> cells = [];

        foreach (CellHandle cellHandle in simulation.GetAllCells()) {
            var position = new Position(cellHandle.Position.X, cellHandle.Position.Y, cellHandle.Position.Z);
            cells.Add(new FrameCell(position, cellHandle.Cell.GetColor()));
        }

        return new Frame(simulation.Cycle, cells);
    }
}
