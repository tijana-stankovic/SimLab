using SimLab.Configuration;
using SimColor = SimLab.Simulator.Color;

namespace SimLab.Simulator;

internal class World {
    // default colors if not specified in config
    private static readonly SimColor s_defaultForegroundColor = new(0, 255, 0); // green
    private static readonly SimColor s_defaultBackgroundColor = new(0, 0, 0); // black
    internal static SimColor DefaultForegroundColor => s_defaultForegroundColor;
    internal static SimColor DefaultBackgroundColor => s_defaultBackgroundColor;

    public int? Id { get; set; }
    public string? Uid { get; set; }
    public string Name { get; set; }
    public int Space { get; set; }
    public int[] Dimensions { get; set; }
    public SimColor ForegroundColor { get; set; }
    public SimColor BackgroundColor { get; set; }
    public long? LastCycle { get; set; }
    public long NextCellId { get; set; }
    public long? LastViewedFrame { get; set; }

    public World(WorldCfg config) {
        Id = null;
        Uid = config.Uid;
        Name = config.Name;
        Space = config.Space;
        Dimensions = config.Dimensions;
        ForegroundColor = BuildColorOrDefault(config.Foreground, s_defaultForegroundColor);
        BackgroundColor = BuildColorOrDefault(config.Background, s_defaultBackgroundColor);
        LastCycle = null;
        NextCellId = 1;
        LastViewedFrame = null;

        // set default color for cells in this world
        Cell.DefaultColor = ForegroundColor;
    }

    private static SimColor BuildColorOrDefault(int[]? rgb, SimColor defaultColor) {
        if (rgb == null || rgb.Length < 3) {
            return defaultColor;
        }

        return new SimColor((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
    }
}
