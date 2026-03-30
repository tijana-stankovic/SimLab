using SimLab.Configuration;

namespace SimLab.Simulator;

internal class World(WorldCfg config) {
    public int? Id { get; set; } = null;
    public string? Uid { get; set; } = config.Uid;
    public string Name { get; set; } = config.Name;
    public int Space { get; set; } = config.Space;
    public int[] Dimensions { get; set; } = config.Dimensions;
}
