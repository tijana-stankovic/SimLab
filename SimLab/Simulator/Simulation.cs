using System.Reflection;

namespace SimLab.Simulator;

internal class Simulation(World world) {
    public World World { get; } = world;
    private readonly Dictionary<Position, Cell> _cells = [];
    public long Cycle { get; set; } = 0;

    public MethodInfo? InitializationMethod { get; set; }
    public MethodInfo? UpdateMethod { get; set; }
    public MethodInfo? EvaluationMethod { get; set; }
    public MethodInfo? ReproductionMethod { get; set; }
    public MethodInfo? SelectionMethod { get; set; }

    public bool TryGetCell(Position pos, out CellHandle? handle) {
        if (_cells.TryGetValue(pos, out var cell)) {
            handle = new CellHandle(pos, cell);
            return true;
        }

        handle = null;
        return false;
    }

    public CellHandle? AddCell(Position pos, Cell cell) {
        if (_cells.ContainsKey(pos))
            return null; // cell already exists at this position

        _cells[pos] = cell;
        return new CellHandle(pos, cell);
    }

    public bool RemoveCell(Position pos) {
        return _cells.Remove(pos);
    }

    public bool MoveCell(Position from, Position to)
    {
        if (!_cells.TryGetValue(from, out var cell))
            return false;

        if (_cells.ContainsKey(to))
            return false; // destination is occupied

        _cells.Remove(from);
        _cells[to] = cell;

        return true;
    }

    public IEnumerable<CellHandle> GetAllCells() {
        return _cells.Select(pair => new CellHandle(pair.Key, pair.Value));
    }
}
