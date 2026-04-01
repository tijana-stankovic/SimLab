using SimLabApi;
using SimLab.Simulator;
using SimLab.Configuration;
using ApiPosition = SimLabApi.Position;
using SimulatorPosition = SimLab.Simulator.Position;

namespace SimLab.ApiImplementation;

internal class API(Simulation? sim) : ISimLabApi {
   private readonly Simulation? _sim = sim;

    public long Cycle {
        get {
            if (_sim == null)
                return 0;
            else
                return _sim.Cycle;
        }
    }

    // return all cells in the world as a list of ICellHandle
    public IEnumerable<ICellHandle> GetAllCells() {
        if (_sim == null)
            return [];
        return _sim.GetAllCells();
    }

    // neighborhood methods
    public IEnumerable<ApiPosition> GetNeighborPositions(ApiPosition pos, NeighborhoodType type = NeighborhoodType.Moore) {
        if (_sim == null)
            return [];

        return _sim.GetNeighborPositions(ToSimulatorPosition(pos), type)
            .Select(ToApiPosition);
    }

    public IEnumerable<ApiPosition> GetNeighborPositions(int x, int y, NeighborhoodType type = NeighborhoodType.Moore) {
        return GetNeighborPositions(new ApiPosition(x, y), type);
    }

    public IEnumerable<ApiPosition> GetNeighborPositions(int x, int y, int z, NeighborhoodType type = NeighborhoodType.Moore) {
        return GetNeighborPositions(new ApiPosition(x, y, z), type);
    }

    public IEnumerable<ICellHandle> GetNeighbors(ApiPosition pos, NeighborhoodType type = NeighborhoodType.Moore) {
        if (_sim == null)
            return [];

        return _sim.GetNeighbors(ToSimulatorPosition(pos), type);
    }

    public IEnumerable<ICellHandle> GetNeighbors(int x, int y, NeighborhoodType type = NeighborhoodType.Moore) {
        return GetNeighbors(new ApiPosition(x, y), type);
    }

    public IEnumerable<ICellHandle> GetNeighbors(int x, int y, int z, NeighborhoodType type = NeighborhoodType.Moore) {
        return GetNeighbors(new ApiPosition(x, y, z), type);
    }

    public int CountNeighbors(ApiPosition pos, NeighborhoodType type = NeighborhoodType.Moore) {
        if (_sim == null)
            return 0;

        return _sim.CountNeighbors(ToSimulatorPosition(pos), type);
    }

    public int CountNeighbors(int x, int y, NeighborhoodType type = NeighborhoodType.Moore) {
        return CountNeighbors(new ApiPosition(x, y), type);
    }

    public int CountNeighbors(int x, int y, int z, NeighborhoodType type = NeighborhoodType.Moore) {
        return CountNeighbors(new ApiPosition(x, y, z), type);
    }

    // cell management methods
    public ICellHandle? TryGetCell(ApiPosition pos) {
        if (_sim == null)
            return null;

        if (_sim.TryGetCell(ToSimulatorPosition(pos), out var handle))
            return handle;

        return null;
    }

    public ICellHandle? TryGetCell(int x, int y, int z = 0) {
        return TryGetCell(new ApiPosition(x, y, z));
    }

    public ICellHandle? GetCurrentCell() {
        if (_sim == null)
            return null;
        return _sim.GetCurrentCell();
    }

    public ICellHandle? AddCell(ApiPosition pos) {
        if (_sim == null)
            return null;

        var cell = new Cell();
        return _sim.AddCell(ToSimulatorPosition(pos), cell);
    }

    public ICellHandle? AddCell(int x, int y, int z = 0) {
        return AddCell(new ApiPosition(x, y, z));
    }

    public bool RemoveCell(ApiPosition pos) {
        if (_sim == null)
            return false;

        return _sim.RemoveCell(ToSimulatorPosition(pos));
    }

    public bool RemoveCell(int x, int y, int z = 0) {
        return RemoveCell(new ApiPosition(x, y, z));
    }

    public bool RemoveCurrentCell() {
        if (_sim == null)
            return false;
        return _sim.RemoveCurrentCell();
    }

    public bool MoveCell(ApiPosition from, ApiPosition to) {
        if (_sim == null)
            return false;

        return _sim.MoveCell(ToSimulatorPosition(from), ToSimulatorPosition(to));
    }

    public bool MoveCell(int fromX, int fromY, int toX, int toY) {
        return MoveCell(new ApiPosition(fromX, fromY), new ApiPosition(toX, toY));
    }

    public bool MoveCell(int fromX, int fromY, int fromZ, int toX, int toY, int toZ) {
        return MoveCell(new ApiPosition(fromX, fromY, fromZ), new ApiPosition(toX, toY, toZ));
    }

    private static SimulatorPosition ToSimulatorPosition(ApiPosition pos) {
        return new SimulatorPosition(pos.X, pos.Y, pos.Z);
    }

    private static ApiPosition ToApiPosition(SimulatorPosition pos) {
        return new ApiPosition(pos.X, pos.Y, pos.Z);
    }

    public string[] GetPlugInMethodParameters(PhaseName simulationPhase) {
        if (_sim == null)
            return [];

        string[] methodParameters = simulationPhase switch {
            PhaseName.Initialization => _sim.InitializationParameters,
            PhaseName.PreCycle => _sim.PreCycleParameters,
            PhaseName.ProcessWorld => _sim.ProcessWorldParameters,
            PhaseName.Update => _sim.UpdateParameters,
            PhaseName.Evaluation => _sim.EvaluationParameters,
            PhaseName.Reproduction => _sim.ReproductionParameters,
            PhaseName.Selection => _sim.SelectionParameters,
            PhaseName.PostCycle => _sim.PostCycleParameters,
            _ => []
        };

        return methodParameters;
    }

    public string[] GetPlugInMethodParameters(string simulationPhase) {
        if (!Phase.TryToValue(simulationPhase, out PhaseName phaseName)) {
            return [];
        }

        return GetPlugInMethodParameters(phaseName);
    }
    
    // TODO: This is just a test method. Remove later.
    public void Test(string callOrigin) {
        Console.WriteLine($"Hello from API method Test (call from {callOrigin}).");
    }
}
