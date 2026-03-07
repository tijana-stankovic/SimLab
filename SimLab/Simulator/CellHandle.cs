using SimLabApi;
using ApiPosition = SimLabApi.Position;

namespace SimLab.Simulator;

internal class CellHandle(Position pos, Cell cell) : ICellHandle {
    public Position Position { get; internal set; } = pos;
    public Cell Cell { get; } = cell;

    // this is necessary to implement interface ICellHandle
    ApiPosition ICellHandle.Position => new(Position.X, Position.Y, Position.Z);
    ICell ICellHandle.Cell => Cell;
}
