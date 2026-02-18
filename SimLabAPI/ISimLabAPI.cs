namespace SimLabApi;

/// <summary>
/// This is the SimLab API - the main interface that the simulation engine exposes to plug-ins.
/// </summary>
public interface ISimLabApi {

    // return all cells in the world as a list of ICellHandle
    IEnumerable<ICellHandle> GetAllCells();

    // cell management methods
    ICellHandle? AddCell(int x, int y, int z = 0);
    bool RemoveCell(int x, int y, int z = 0);
    ICellHandle? TryGetCell(int x, int y, int z = 0);


    long Cycle { get; }

    public string[] GetPlugInMethodParameters(string simulationPhase);

    // TODO: This is just a test method. Remove later.
    void Test(string callOrigin);
}
