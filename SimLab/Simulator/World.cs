using SimLab.Configuration;

namespace SimLab.Simulator;

internal class World(WorldCfg config) {
    public string Name { get; set; } = config.Name;
    public int Space { get; set; } = config.Space;
    public int[] Dimensions { get; set; } = config.Dimensions;
}