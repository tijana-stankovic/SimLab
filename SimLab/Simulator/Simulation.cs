using SimLabApi;
using System.Reflection;

namespace SimLab.Simulator;

internal class Simulation {
    private static readonly (int dx, int dy)[] s_vonNeumannOffsets2D = [
        (0, -1),
        (-1, 0),
        (1, 0),
        (0, 1)
    ];

    private static readonly (int dx, int dy)[] s_mooreOffsets2D = [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),            (1, 0),
        (-1, 1),  (0, 1),   (1, 1)
    ];

    private static readonly (int dx, int dy, int dz)[] s_vonNeumannOffsets3D = [
        (1, 0, 0),
        (-1, 0, 0),
        (0, 1, 0),
        (0, -1, 0),
        (0, 0, 1),
        (0, 0, -1)
    ];

    private static readonly (int dx, int dy, int dz)[] s_mooreOffsets3D = BuildMooreOffsets3D();

    public bool IsRunning { get; set; } = false;
    private long _cycle = 0;
    private long _nextCellId = 1;

    private SimulationMode _mode = SimulationMode.SynchronousCA;

    public World World { get; }

    private Dictionary<Position, Cell> _cellsCurrent = [];
    private Dictionary<Position, Cell> _cellsNext = [];
    private Dictionary<Position, Cell> ReadBuffer => _cellsCurrent;
    private Dictionary<Position, Cell> WriteBuffer => Mode == SimulationMode.Asynchronous ? _cellsCurrent : _cellsNext;

    private CellHandle? _currentCell;

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
    public MethodInfo? PreCycleMethod { get; set; }
    public string[] PreCycleParameters { get; set; } = [];
    public MethodInfo? ProcessWorldMethod { get; set; }
    public string[] ProcessWorldParameters { get; set; } = [];
    public MethodInfo? UpdateMethod { get; set; }
    public string[] UpdateParameters { get; set; } = [];
    public MethodInfo? EvaluationMethod { get; set; }
    public string[] EvaluationParameters { get; set; } = [];
    public MethodInfo? ReproductionMethod { get; set; }
    public string[] ReproductionParameters { get; set; } = [];
    public MethodInfo? SelectionMethod { get; set; }
    public string[] SelectionParameters { get; set; } = [];
    public MethodInfo? PostCycleMethod { get; set; }
    public string[] PostCycleParameters { get; set; } = [];

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

        cell.SetId(_nextCellId++);
        WriteBuffer[pos] = cell;
        return new CellHandle(pos, cell);
    }

    public bool RemoveCell(Position pos) {
        return WriteBuffer.Remove(pos);
    }

    public bool RemoveCurrentCell() {
        if (_currentCell == null)
            return false;

        bool removed = WriteBuffer.Remove(_currentCell.Position);
        if (removed) {
            ClearCurrentCell();
        }

        return removed;
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

    public int GetCellCount() {
        return ReadBuffer.Count;
    }

    // this method provides direct and fast access to the cell data without creating CellHandle objects
    public IEnumerable<KeyValuePair<Position, Cell>> GetAllCellsDirect() {
        return ReadBuffer;
    }

    public long GetNextCellId() {
        return _nextCellId;
    }

    public bool SetCurrentState(long cycle, long nextCellId, IEnumerable<CellHandle> cells, out string? error) {
        ClearCurrentCell();
        _cellsCurrent.Clear();
        _cellsNext.Clear();

        foreach (CellHandle cellHandle in cells) {
            long cellId = cellHandle.Cell.GetId();
            Cell cell = new();
            cell.SetId(cellId);

            try {
                for (int i = 0; i < Characteristics.Count; i++) {
                    cell[i] = cellHandle.Cell[i];
                }
            } catch (Exception ex) {
                error = $"Invalid characteristic values for cell at {cellHandle.Position}: {ex.Message}";
                return false;
            }

            _cellsCurrent[cellHandle.Position] = cell;
        }

        Cycle = cycle;
        _nextCellId = nextCellId;
        IsRunning = true;

        error = null;
        return true;
    }

    public IEnumerable<Position> GetNeighborPositions(Position pos, NeighborhoodType type = NeighborhoodType.Moore) {
        return World.Space == 2
            ? GetNeighborPositions2D(pos, type)
            : GetNeighborPositions3D(pos, type);
    }

    public IEnumerable<CellHandle> GetNeighbors(Position pos, NeighborhoodType type = NeighborhoodType.Moore) {
        foreach (Position neighborPos in GetNeighborPositions(pos, type)) {
            if (ReadBuffer.TryGetValue(neighborPos, out var cell)) {
                yield return new CellHandle(neighborPos, cell);
            }
        }
    }

    public int CountNeighbors(Position pos, NeighborhoodType type = NeighborhoodType.Moore) {
        int count = 0;

        foreach (Position neighborPos in GetNeighborPositions(pos, type)) {
            if (ReadBuffer.ContainsKey(neighborPos)) {
                count++;
            }
        }

        return count;
    }

    private IEnumerable<Position> GetNeighborPositions2D(Position pos, NeighborhoodType type) {
        var offsets = type == NeighborhoodType.VonNeumann
            ? s_vonNeumannOffsets2D
            : s_mooreOffsets2D;

        foreach (var (dx, dy) in offsets) {
            yield return new Position(pos.X + dx, pos.Y + dy, pos.Z);
        }
    }

    private IEnumerable<Position> GetNeighborPositions3D(Position pos, NeighborhoodType type) {
        var offsets = type == NeighborhoodType.VonNeumann
            ? s_vonNeumannOffsets3D
            : s_mooreOffsets3D;

        foreach (var (dx, dy, dz) in offsets) {
            yield return new Position(pos.X + dx, pos.Y + dy, pos.Z + dz);
        }
    }

    private static (int dx, int dy, int dz)[] BuildMooreOffsets3D() {
        List<(int dx, int dy, int dz)> offsets = [];

        for (int dx = -1; dx <= 1; dx++) {
            for (int dy = -1; dy <= 1; dy++) {
                for (int dz = -1; dz <= 1; dz++) {
                    if (dx == 0 && dy == 0 && dz == 0)
                        continue;

                    offsets.Add((dx, dy, dz));
                }
            }
        }

        return offsets.ToArray();
    }
}
