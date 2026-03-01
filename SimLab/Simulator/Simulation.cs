using System.Reflection;

namespace SimLab.Simulator;

internal class Simulation {
    public bool IsRunning { get; set; } = false;

    private SimulationMode _mode = SimulationMode.SynchronousCA;
    public World World { get; }
    private Dictionary<Position, Cell> _cellsCurrent = [];
    private Dictionary<Position, Cell> _cellsNext = [];
    private long _cycle = 0;
    private CellHandle? _currentCell;

    private Dictionary<Position, Cell> ReadBuffer => _cellsCurrent;
    private Dictionary<Position, Cell> WriteBuffer => Mode == SimulationMode.Asynchronous ? _cellsCurrent : _cellsNext;

    public Simulation(World world) {
        World = world;
        Cell.ActiveWriteCycle = _cycle;
        Mode = SimulationMode.SynchronousCA;
    }

    public SimulationMode Mode {
        get => _mode;
        set {
            _mode = value;
            Cell.SkipWriteAccessCheck = (_mode == SimulationMode.Asynchronous);
            ClearCurrentCell();
        }
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

    public void BeginCycle() {
        ClearCurrentCell();

        if (Mode == SimulationMode.SynchronousCA) {
            _cellsNext.Clear();
            foreach (var pair in _cellsCurrent) {
                _cellsNext[pair.Key] = pair.Value.Clone();
            }
        }
    }

    public void EndCycle() {
        ClearCurrentCell();

        if (Mode == SimulationMode.SynchronousCA) {
            (_cellsCurrent, _cellsNext) = (_cellsNext, _cellsCurrent);
        }
    }

    public bool SetCurrentCell(Position pos) {
        if (WriteBuffer.TryGetValue(pos, out var cell)) {
            _currentCell = new CellHandle(pos, cell);
            return true;
        }

        ClearCurrentCell();
        return false;
    }

    public bool SetCurrentCell(CellHandle cellHandle) {
        _currentCell = cellHandle;
        return true;
    }

    public CellHandle? GetCurrentCell() {
        return _currentCell;
    }

    public void ClearCurrentCell() {
        _currentCell = null;
    }

    public bool TryGetCell(Position pos, out CellHandle? handle) {
        if (ReadBuffer.TryGetValue(pos, out var cell)) {
            handle = new CellHandle(pos, cell);
            return true;
        }

        handle = null;
        return false;
    }

    public CellHandle? AddCell(Position pos, Cell cell) {
        if (WriteBuffer.ContainsKey(pos))
            return null; // cell already exists at this position

        WriteBuffer[pos] = cell;
        return new CellHandle(pos, cell);
    }

    public bool RemoveCell(Position pos) {
        return WriteBuffer.Remove(pos);
    }

    public bool MoveCell(Position from, Position to)
    {
        if (!WriteBuffer.TryGetValue(from, out var cell))
            return false;

        if (WriteBuffer.ContainsKey(to))
            return false; // destination is occupied

        WriteBuffer.Remove(from);
        WriteBuffer[to] = cell;

        return true;
    }

    public IEnumerable<CellHandle> GetAllCells() {
        return ReadBuffer.Select(pair => new CellHandle(pair.Key, pair.Value));
    }
}
