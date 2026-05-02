namespace SimLab.Configuration;

internal class WorldCfg {
    public string? Uid { get; init; }
    public required string Name { get; init; }
    public required int Space { get; init; }
    public required int[] Dimensions { get; init; }
    public string[] Characteristics { get; init; } = [];
    public string[] Globals { get; init; } = [];
    public string? Mode { get; init; }
    public string? Boundary { get; init; }
    public string[]? Boundaries { get; init; }
    public int[]? Foreground { get; init; }
    public int[]? Background { get; init; }
    public MethodCfg? Initialization { get; init; }
    public MethodCfg? PreCycle { get; init; }
    public MethodCfg? ProcessWorld { get; init; }
    public MethodCfg? Update { get; init; }
    public MethodCfg? Evaluation { get; init; }
    public MethodCfg? Reproduction { get; init; }
    public MethodCfg? Selection { get; init; }
    public MethodCfg? PostCycle { get; init; }
}
