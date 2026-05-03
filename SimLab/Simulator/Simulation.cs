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
    public ApiStatus LastApiStatus { get; set; } = ApiStatus.Ok;

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
        Position realPos = ApplyCyclicBoundary(pos);
        if (!IsValidPosition(realPos)) {
            ClearCurrentCell();
            LastApiStatus = ApiStatus.OutOfWorld;
            return false;
        }

        if (WriteBuffer.TryGetValue(realPos, out var cell)) {
            _currentCell = new CellHandle(realPos, cell);
            LastApiStatus = ApiStatus.Ok;
            return true;
        }

        ClearCurrentCell();
        LastApiStatus = ApiStatus.CellNotFound;
        return false;
    }

    public bool SetCurrentCell(CellHandle cellHandle) {
        _currentCell = cellHandle;
        LastApiStatus = ApiStatus.Ok;
        return true;
    }

    public CellHandle? GetCurrentCell() {
        LastApiStatus = _currentCell == null ? ApiStatus.CurrentCellNotSet : ApiStatus.Ok;
        return _currentCell;
    }

    public void ClearCurrentCell() {
        _currentCell = null;
    }

    // get cell at position from THE CURRENT STATE of the world, 
    // return null if no cell exists at that position
    public bool TryGetCell(Position pos, out CellHandle? handle) {
        Position realPos = ApplyCyclicBoundary(pos);
        if (!IsValidPosition(realPos)) {
            handle = null;
            LastApiStatus = ApiStatus.OutOfWorld;
            return false;
        }

        if (ReadBuffer.TryGetValue(realPos, out var cell)) {
            handle = new CellHandle(realPos, cell);
            LastApiStatus = ApiStatus.Ok;
            return true;
        }

        handle = null;
        LastApiStatus = ApiStatus.CellNotFound;
        return false;
    }

    // get cell at position from THE NEXT STATE of the world, 
    // return null if no cell exists at that position
    public bool TryGetCellNext(Position pos, out CellHandle? handle) {
        Position realPos = ApplyCyclicBoundary(pos);
        if (!IsValidPosition(realPos)) {
            handle = null;
            LastApiStatus = ApiStatus.OutOfWorld;
            return false;
        }

        if (WriteBuffer.TryGetValue(realPos, out var cell)) {
            handle = new CellHandle(realPos, cell);
            LastApiStatus = ApiStatus.Ok;
            return true;
        }

        handle = null;
        LastApiStatus = ApiStatus.CellNotFound;
        return false;
    }

    public CellHandle? AddCell(Position pos, Cell cell) {
        Position realPos = ApplyCyclicBoundary(pos);
        if (!IsValidPosition(realPos)) {
            LastApiStatus = ApiStatus.OutOfWorld;
            return null;
        }

        if (WriteBuffer.ContainsKey(realPos)) {
            LastApiStatus = ApiStatus.PositionOccupied;
            return null; // cell already exists at this position
        }

        cell.SetId(_nextCellId++);
        WriteBuffer[realPos] = cell;
        LastApiStatus = ApiStatus.Ok;
        return new CellHandle(realPos, cell);
    }

    public bool RemoveCell(Position pos) {
        Position realPos = ApplyCyclicBoundary(pos);
        if (!IsValidPosition(realPos)) {
            LastApiStatus = ApiStatus.OutOfWorld;
            return false;
        }

        bool removed = WriteBuffer.Remove(realPos);
        LastApiStatus = removed ? ApiStatus.Ok : ApiStatus.CellNotFound;
        return removed;
    }

    public bool RemoveCurrentCell() {
        if (_currentCell == null) {
            LastApiStatus = ApiStatus.CurrentCellNotSet;
            return false;
        }

        bool removed = WriteBuffer.Remove(_currentCell.Position);
        if (removed) {
            ClearCurrentCell();
            LastApiStatus = ApiStatus.Ok;
        } else {
            LastApiStatus = ApiStatus.CellNotFound;
        }

        return removed;
    }

    public bool MoveCell(Position from, Position to)
    {
        Position realFrom = ApplyCyclicBoundary(from);
        if (!IsValidPosition(realFrom)) {
            LastApiStatus = ApiStatus.OutOfWorld;
            return false;
        }

        Position realTo = ApplyCyclicBoundary(to);
        realTo = ApplyBlockingBoundary(realTo);

        if (!WriteBuffer.TryGetValue(realFrom, out var cell)) {
            LastApiStatus = ApiStatus.CellNotFound;
            return false;
        }

        if (!IsValidPosition(realTo)) {
            WriteBuffer.Remove(realFrom);
            LastApiStatus = ApiStatus.Ok;
            return true;
        }

        if (WriteBuffer.ContainsKey(realTo)) {
            LastApiStatus = ApiStatus.DestinationOccupied;
            return false; // destination is occupied
        }

        WriteBuffer.Remove(realFrom);
        WriteBuffer[realTo] = cell;
        LastApiStatus = ApiStatus.Ok;

        return true;
    }

    public IEnumerable<CellHandle> GetAllCells() {
        LastApiStatus = ApiStatus.Ok;
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
        Position realPos = ApplyCyclicBoundary(pos);
        if (!IsValidPosition(realPos)) {
            LastApiStatus = ApiStatus.OutOfWorld;
            return [];
        }

        LastApiStatus = ApiStatus.Ok;
        return World.Space == 2
            ? GetNeighborPositions2D(realPos, type)
            : GetNeighborPositions3D(realPos, type);
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
        HashSet<Position> uniquePositions = [];

        foreach (var (dx, dy) in offsets) {
            Position neighborPos = new(pos.X + dx, pos.Y + dy, pos.Z);
            Position realPos = ApplyCyclicBoundary(neighborPos);
            if (IsValidPosition(realPos) && uniquePositions.Add(realPos)) {
                yield return realPos;
            }
        }
    }

    private IEnumerable<Position> GetNeighborPositions3D(Position pos, NeighborhoodType type) {
        var offsets = type == NeighborhoodType.VonNeumann
            ? s_vonNeumannOffsets3D
            : s_mooreOffsets3D;
        HashSet<Position> uniquePositions = [];

        foreach (var (dx, dy, dz) in offsets) {
            Position neighborPos = new(pos.X + dx, pos.Y + dy, pos.Z + dz);
            Position realPos = ApplyCyclicBoundary(neighborPos);
            if (IsValidPosition(realPos) && uniquePositions.Add(realPos)) {
                yield return realPos;
            }
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

    private Position ApplyCyclicBoundary(Position pos) {
        int x = pos.X;
        int y = pos.Y;
        int z = pos.Z;

        if (World.Dimensions.Length > 0 && World.Dimensions[0] > 0 && World.BoundaryX == BoundaryMode.Cyclic)
            x = WrapCoordinate(x, World.Dimensions[0]);

        if (World.Dimensions.Length > 1 && World.Dimensions[1] > 0 && World.BoundaryY == BoundaryMode.Cyclic)
            y = WrapCoordinate(y, World.Dimensions[1]);

        if (World.Dimensions.Length > 2 && World.Dimensions[2] > 0 && World.BoundaryZ == BoundaryMode.Cyclic)
            z = WrapCoordinate(z, World.Dimensions[2]);

        return new Position(x, y, z);
    }

    private static int WrapCoordinate(int value, int dimension) {
        int wrapped = value % dimension;
        if (wrapped < 0)
            wrapped += dimension;

        return wrapped;
    }

    private Position ApplyBlockingBoundary(Position pos) {
        int x = pos.X;
        int y = pos.Y;
        int z = pos.Z;

        if (World.Dimensions.Length > 0 && World.Dimensions[0] > 0 && World.BoundaryX == BoundaryMode.Blocking)
            x = LimitCoordinate(x, World.Dimensions[0]);

        if (World.Dimensions.Length > 1 && World.Dimensions[1] > 0 && World.BoundaryY == BoundaryMode.Blocking)
            y = LimitCoordinate(y, World.Dimensions[1]);

        if (World.Dimensions.Length > 2 && World.Dimensions[2] > 0 && World.BoundaryZ == BoundaryMode.Blocking)
            z = LimitCoordinate(z, World.Dimensions[2]);

        return new Position(x, y, z);
    }

    private static int LimitCoordinate(int value, int dimension) {
        if (value < 0)
            return 0;

        if (value >= dimension)
            return dimension - 1;

        return value;
    }

    private bool IsValidPosition(Position pos) {
        if (World.Dimensions.Length > 0 && World.Dimensions[0] > 0) {
            if (pos.X < 0 || pos.X >= World.Dimensions[0])
                return false;
        }

        if (World.Dimensions.Length > 1 && World.Dimensions[1] > 0) {
            if (pos.Y < 0 || pos.Y >= World.Dimensions[1])
                return false;
        }

        if (World.Dimensions.Length > 2 && World.Dimensions[2] > 0) {
            if (pos.Z < 0 || pos.Z >= World.Dimensions[2])
                return false;
        }

        return true;
    }
}
