using SimLabApi;
using SimLab.Simulator;

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

    public ICellHandle? GetCurrentCell() {
        if (_sim == null)
            return null;
        return _sim.GetCurrentCell();
    }

    public ICellHandle? AddCell(int x, int y, int z = 0) {
        var pos = new Position(x, y, z);
        var cell = new Cell();
        if (_sim == null)
            return null;
        var handle = _sim.AddCell(pos, cell);
        return handle;
    }

    public bool RemoveCell(int x, int y, int z = 0) {
        if (_sim == null)
            return false;
        return _sim.RemoveCell(new Position(x, y, z));
    }

    public ICellHandle? TryGetCell(int x, int y, int z = 0) {
        if (_sim == null)
            return null;
        if (_sim.TryGetCell(new Position(x, y, z), out var handle))
            return handle;
        return null;
    }

    public string[] GetPlugInMethodParameters(string simulationPhase) {
        if (_sim == null)
            return [];

        string[] methodParameters = simulationPhase.ToLower() switch {
            "initialization" => _sim.InitializationParameters,
            "update" => _sim.UpdateParameters,
            "evaluation" => _sim.EvaluationParameters,
            "reproduction" => _sim.ReproductionParameters,
            "selection" => _sim.SelectionParameters,
            _ => []
        };

        return methodParameters;
    }

    // TODO: This is just a test method. Remove later.
    public void Test(string callOrigin) {
        Console.WriteLine($"Hello from API method Test (call from {callOrigin}).");
    }
}
