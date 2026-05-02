namespace SimLabApi;

public enum ApiStatus {
    Ok = 0,
    NoActiveSimulation,
    CellNotFound,
    CurrentCellNotSet,
    PositionOccupied,
    DestinationOccupied,
    InvalidPhaseName
}
