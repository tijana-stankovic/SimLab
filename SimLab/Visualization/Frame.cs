using SimLab.Simulator;
using SimColor = SimLab.Simulator.Color;

namespace SimLab.Visualization;

internal class Frame {
    public long Cycle { get; }
    public IReadOnlyList<FrameCell> Cells { get; }
    public SimColor ForegroundColor { get; }
    public SimColor BackgroundColor { get; }

    public Frame(long cycle, List<FrameCell> cells, SimColor foregroundColor, SimColor backgroundColor) {
        Cycle = cycle;
        Cells = cells;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
    }

    public static Frame CreateFromSimulation(Simulation simulation) {
        List<FrameCell> cells = [];

        foreach (CellHandle cellHandle in simulation.GetAllCells()) {
            var position = new Position(cellHandle.Position.X, cellHandle.Position.Y, cellHandle.Position.Z);
            cells.Add(new FrameCell(position, cellHandle.Cell.GetColor()));
        }

        return new Frame(
            simulation.Cycle,
            cells,
            simulation.World.ForegroundColor,
            simulation.World.BackgroundColor);
    }
}
