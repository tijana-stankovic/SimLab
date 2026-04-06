namespace SimLab.DB;

internal class DbWorldInfo {
    public required int Id { get; init; }
    public required string Uid { get; init; }
    public required string Name { get; init; }
    public required int Space { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Z { get; init; }
    public required char Mode { get; init; }
    public long? LastCycle { get; init; }
    public required long NextCellId { get; init; }
    public long? LastViewedFrame { get; init; }
}
