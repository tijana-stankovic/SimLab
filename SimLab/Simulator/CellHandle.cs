using SimLabApi;

namespace SimLab.Simulator;

internal class CellHandle(Position pos, Cell cell) : ICellHandle {
    public Position Position { get; internal set; } = pos;
    public Cell Cell { get; } = cell;

    // this is necessary to implement interface ICellHandle
    IPosition ICellHandle.Position => Position;
    ICell ICellHandle.Cell => Cell;
}
