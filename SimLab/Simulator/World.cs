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

    // system global characteristics names
    internal const string ForegroundRName = "_foreground_r";
    internal const string ForegroundGName = "_foreground_g";
    internal const string ForegroundBName = "_foreground_b";
    internal const string BackgroundRName = "_background_r";
    internal const string BackgroundGName = "_background_g";
    internal const string BackgroundBName = "_background_b";

    // list of system global characteristics names
    private static readonly string[] s_systemGlobalNames = [
        ForegroundRName,
        ForegroundGName,
        ForegroundBName,
        BackgroundRName,
        BackgroundGName,
        BackgroundBName
    ];

    // global values (user plus system at the end)
    private readonly float[] _globalValues;

    public long? LastCycle { get; set; }
    public long NextCellId { get; set; }
    public long? LastViewedFrame { get; set; }

    public World(WorldCfg config) {
        Id = null;
        Uid = config.Uid;
        Name = config.Name;
        Space = config.Space;
        Dimensions = config.Dimensions;
        _globalValues = new float[Globals.Count];
        ForegroundColor = BuildColorOrDefault(config.Foreground, s_defaultForegroundColor);
        BackgroundColor = BuildColorOrDefault(config.Background, s_defaultBackgroundColor);
        LastCycle = null;
        NextCellId = 1;
        LastViewedFrame = null;

        // set default color for cells in this world
        Cell.DefaultColor = ForegroundColor;
    }

    // access global values by index
    public float this[int index] {
        get => _globalValues[index];
        set {
            _globalValues[index] = value;
            // this ensures that if the foreground color globals are changed directly by index, 
            // the Cell.DefaultColor is updated 
            if (index == Globals.GetIndex(ForegroundRName) ||
                index == Globals.GetIndex(ForegroundGName) ||
                index == Globals.GetIndex(ForegroundBName)) {
                Cell.DefaultColor = ForegroundColor;
            }
        }
    }

    // access global values by name
    public float this[string name] {
        get => _globalValues[Globals.GetIndex(name)];
        set {
            _globalValues[Globals.GetIndex(name)] = value;
            // this ensures that if the foreground color globals are changed directly by name,
            // the Cell.DefaultColor is updated
            if (name.Equals(ForegroundRName, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(ForegroundGName, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(ForegroundBName, StringComparison.OrdinalIgnoreCase)) {
                Cell.DefaultColor = ForegroundColor;
            }
        }
    }

    public SimColor ForegroundColor {
        get => new((byte)this[ForegroundRName], (byte)this[ForegroundGName], (byte)this[ForegroundBName]);
        set {
            this[ForegroundRName] = value.R;
            this[ForegroundGName] = value.G;
            this[ForegroundBName] = value.B;
        }
    }

    public SimColor BackgroundColor {
        get => new((byte)this[BackgroundRName], (byte)this[BackgroundGName], (byte)this[BackgroundBName]);
        set {
            this[BackgroundRName] = value.R;
            this[BackgroundGName] = value.G;
            this[BackgroundBName] = value.B;
        }
    }

    private static SimColor BuildColorOrDefault(int[]? rgb, SimColor defaultColor) {
        if (rgb == null || rgb.Length < 3) {
            return defaultColor;
        }

        return new SimColor((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
    }

    internal static string[] MergeWithSystemGlobals(string[] globals) {
        var merged = new List<string>(globals);
        merged.AddRange(s_systemGlobalNames);

        return merged.ToArray();
    }
}
