using System.Reflection;

namespace SimLab.Simulator;

internal class Simulation {
    public bool IsRunning { get; set; } = false;

    public World World { get; }
    private readonly Dictionary<Position, Cell> _cellsCurrent = [];
    private readonly Dictionary<Position, Cell> _cellsNext = [];
    private long _cycle = 0;

    public Simulation(World world) {
        World = world;
        Cell.ActiveWriteCycle = _cycle;
    }

    public long Cycle {
        get => _cycle;
        set {
            _cycle = value;
            Cell.ActiveWriteCycle = value;
        }
    }

    public MethodInfo? InitializationMethod { get; set; }
    public string[] InitializationParameters { get; set; } = [];
    public MethodInfo? UpdateMethod { get; set; }
    public string[] UpdateParameters { get; set; } = [];

    public MethodInfo? EvaluationMethod { get; set; }
    public string[] EvaluationParameters { get; set; } = [];
    public MethodInfo? ReproductionMethod { get; set; }
    public string[] ReproductionParameters { get; set; } = [];
    public MethodInfo? SelectionMethod { get; set; }
    public string[] SelectionParameters { get; set; } = [];

    public bool TryGetCell(Position pos, out CellHandle? handle) {
        if (_cellsCurrent.TryGetValue(pos, out var cell)) {
            handle = new CellHandle(pos, cell);
            return true;
        }

        handle = null;
        return false;
    }

    public CellHandle? AddCell(Position pos, Cell cell) {
        if (_cellsCurrent.ContainsKey(pos))
            return null; // cell already exists at this position

        _cellsCurrent[pos] = cell;
        return new CellHandle(pos, cell);
    }

    public bool RemoveCell(Position pos) {
        return _cellsCurrent.Remove(pos);
    }

    public bool MoveCell(Position from, Position to)
    {
        if (!_cellsCurrent.TryGetValue(from, out var cell))
            return false;

        if (_cellsCurrent.ContainsKey(to))
            return false; // destination is occupied

        _cellsCurrent.Remove(from);
        _cellsCurrent[to] = cell;

        return true;
    }

    public IEnumerable<CellHandle> GetAllCells() {
        return _cellsCurrent.Select(pair => new CellHandle(pair.Key, pair.Value));
    }
}
