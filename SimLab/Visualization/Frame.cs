using SimLab.Simulator;

namespace SimLab.Visualization;

internal class Frame {
    public long Cycle { get; }
    public IReadOnlyList<Position> Cells { get; }

    public Frame(long cycle, List<Position> cells) {
        Cycle = cycle;
        Cells = cells;
    }

    public static Frame CreateFromSimulation(Simulation simulation) {
        List<Position> cells = [];

        foreach (CellHandle cellHandle in simulation.GetAllCells()) {
            cells.Add(new Position(cellHandle.Position.X, cellHandle.Position.Y, cellHandle.Position.Z));
        }

        return new Frame(simulation.Cycle, cells);
    }
}
