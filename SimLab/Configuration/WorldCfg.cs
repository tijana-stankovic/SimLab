namespace SimLab.Configuration;

internal class WorldCfg {
    public required string Name { get; init; }
    public required int Space { get; init; }
    public required int[] Dimensions { get; init; }
    public required string[] Characteristics { get; init; }
    public MethodCfg? Initialization { get; init; }
    public MethodCfg? Update { get; init; }
    public MethodCfg? Evaluation { get; init; }
    public MethodCfg? Reproduction { get; init; }
    public MethodCfg? Selection { get; init; }
}