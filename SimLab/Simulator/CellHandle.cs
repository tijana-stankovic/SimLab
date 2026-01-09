namespace SimLab.Simulator;

internal class CellHandle(Position pos, Cell cell) {
    public Position Position { get; internal set; } = pos;
    public Cell Cell { get; } = cell;
}
