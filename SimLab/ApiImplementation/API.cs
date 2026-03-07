using SimLabApi;
using SimLab.Simulator;
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

    public bool MoveCell(int fromX, int fromY, int fromZ, int toX, int toY, int toZ) {
        return MoveCell(new ApiPosition(fromX, fromY, fromZ), new ApiPosition(toX, toY, toZ));
    }

    public string[] GetPlugInMethodParameters(string simulationPhase) {
        if (_sim == null)
            return [];

        string[] methodParameters = simulationPhase.ToLower() switch {
            "initialization" => _sim.InitializationParameters,
            "precycle" => _sim.PreCycleParameters,
            "processworld" => _sim.ProcessWorldParameters,
            "update" => _sim.UpdateParameters,
            "evaluation" => _sim.EvaluationParameters,
            "reproduction" => _sim.ReproductionParameters,
            "selection" => _sim.SelectionParameters,
            "postcycle" => _sim.PostCycleParameters,
            _ => []
        };

        return methodParameters;
    }

    // TODO: This is just a test method. Remove later.
    public void Test(string callOrigin) {
        Console.WriteLine($"Hello from API method Test (call from {callOrigin}).");
    }

    private static SimulatorPosition ToSimulatorPosition(ApiPosition pos) {
        return new SimulatorPosition(pos.X, pos.Y, pos.Z);
    }
}
