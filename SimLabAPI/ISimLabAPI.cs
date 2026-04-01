namespace SimLabApi;

/// <summary>
/// This is the SimLab API - the main interface that the simulation engine exposes to plug-ins.
/// </summary>
public interface ISimLabApi {

    // return all cells in the world as a list of ICellHandle
    IEnumerable<ICellHandle> GetAllCells();

    // neighborhood methods
    IEnumerable<Position> GetNeighborPositions(Position pos, NeighborhoodType type = NeighborhoodType.Moore);
    IEnumerable<Position> GetNeighborPositions(int x, int y, NeighborhoodType type = NeighborhoodType.Moore);
    IEnumerable<Position> GetNeighborPositions(int x, int y, int z, NeighborhoodType type = NeighborhoodType.Moore);
    IEnumerable<ICellHandle> GetNeighbors(Position pos, NeighborhoodType type = NeighborhoodType.Moore);
    IEnumerable<ICellHandle> GetNeighbors(int x, int y, NeighborhoodType type = NeighborhoodType.Moore);
    IEnumerable<ICellHandle> GetNeighbors(int x, int y, int z, NeighborhoodType type = NeighborhoodType.Moore);
    int CountNeighbors(Position pos, NeighborhoodType type = NeighborhoodType.Moore);
    int CountNeighbors(int x, int y, NeighborhoodType type = NeighborhoodType.Moore);
    int CountNeighbors(int x, int y, int z, NeighborhoodType type = NeighborhoodType.Moore);

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
    bool MoveCell(int fromX, int fromY, int toX, int toY);
    bool MoveCell(int fromX, int fromY, int fromZ, int toX, int toY, int toZ);


    long Cycle { get; }

    public string[] GetPlugInMethodParameters(PhaseName simulationPhase);
    public string[] GetPlugInMethodParameters(string simulationPhase);

    // TODO: This is just a test method. Remove later.
    void Test(string callOrigin);
}
