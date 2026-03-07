namespace SimLabApi;

/// <summary>
/// This is the SimLab API - the main interface that the simulation engine exposes to plug-ins.
/// </summary>
public interface ISimLabApi {

    // return all cells in the world as a list of ICellHandle
    IEnumerable<ICellHandle> GetAllCells();

    // cell management methods
    ICellHandle? TryGetCell(Position pos);
    ICellHandle? TryGetCell(int x, int y, int z = 0);
    ICellHandle? GetCurrentCell();
    ICellHandle? AddCell(Position pos);
    ICellHandle? AddCell(int x, int y, int z = 0);
    bool RemoveCell(Position pos);
    bool RemoveCell(int x, int y, int z = 0);
    bool RemoveCurrentCell();
    bool MoveCell(Position from, Position to);
    bool MoveCell(int fromX, int fromY, int fromZ, int toX, int toY, int toZ);


    long Cycle { get; }

    public string[] GetPlugInMethodParameters(string simulationPhase);

    // TODO: This is just a test method. Remove later.
    void Test(string callOrigin);
}
