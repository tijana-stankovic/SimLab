namespace SimLabApi;

public enum ApiStatus {
    Ok = 0,
    NoActiveSimulation,
    InvalidPhaseName,
    CellNotFound,
    CurrentCellNotSet,
    PositionOccupied,
    DestinationOccupied,
    OutOfWorld
}
