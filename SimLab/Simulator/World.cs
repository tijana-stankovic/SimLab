using SimLab.Configuration;
using SimLabApi;
using SimColor = SimLab.Simulator.Color;

namespace SimLab.Simulator;

internal class World : IGlobals {
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
    internal const string BoundaryXName = "_boundary_x";
    internal const string BoundaryYName = "_boundary_y";
    internal const string BoundaryZName = "_boundary_z";

    // list of system global characteristics names
    private static readonly string[] s_systemGlobalNames = [
        ForegroundRName,
        ForegroundGName,
        ForegroundBName,
        BackgroundRName,
        BackgroundGName,
        BackgroundBName,
        BoundaryXName,
        BoundaryYName,
        BoundaryZName
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
        InitializeBoundaryModes(config.Boundary, config.Boundaries);
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

    public BoundaryMode BoundaryX {
        get => ValueToBoundaryMode(this[BoundaryXName]);
        set => this[BoundaryXName] = (int)value;
    }

    public BoundaryMode BoundaryY {
        get => ValueToBoundaryMode(this[BoundaryYName]);
        set => this[BoundaryYName] = (int)value;
    }

    public BoundaryMode BoundaryZ {
        get => ValueToBoundaryMode(this[BoundaryZName]);
        set => this[BoundaryZName] = (int)value;
    }

    private static SimColor BuildColorOrDefault(int[]? rgb, SimColor defaultColor) {
        if (rgb == null || rgb.Length < 3) {
            return defaultColor;
        }

        return new SimColor((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
    }

    private void InitializeBoundaryModes(string? boundary, string[]? boundaries) {
        BoundaryX = BoundaryMode.Void;
        BoundaryY = BoundaryMode.Void;
        BoundaryZ = BoundaryMode.Void;

        if (boundaries != null) {
            if (boundaries.Length > 0 && TryParseBoundaryMode(boundaries[0], out BoundaryMode boundaryX)) {
                BoundaryX = boundaryX;
            }

            if (boundaries.Length > 1 && TryParseBoundaryMode(boundaries[1], out BoundaryMode boundaryY)) {
                BoundaryY = boundaryY;
            }

            if (boundaries.Length > 2 && TryParseBoundaryMode(boundaries[2], out BoundaryMode boundaryZ)) {
                BoundaryZ = boundaryZ;
            }
        }

        if (TryParseBoundaryMode(boundary, out BoundaryMode globalBoundary)) {
            BoundaryX = globalBoundary;
            BoundaryY = globalBoundary;
            BoundaryZ = globalBoundary;
        }
    }

    private static bool TryParseBoundaryMode(string? boundaryText, out BoundaryMode boundaryMode) {
        boundaryMode = BoundaryMode.Void;
        if (string.IsNullOrWhiteSpace(boundaryText)) {
            return false;
        }

        switch (boundaryText.Trim().ToUpperInvariant()) {
            case "VOID":
                boundaryMode = BoundaryMode.Void;
                return true;
            case "CYCLIC":
                boundaryMode = BoundaryMode.Cyclic;
                return true;
            case "BLOCKING":
                boundaryMode = BoundaryMode.Blocking;
                return true;
            default:
                return false;
        }
    }

    private static BoundaryMode ValueToBoundaryMode(float value) {
        int modeValue = (int)value;
        switch (modeValue) {
            case (int)BoundaryMode.Cyclic:
                return BoundaryMode.Cyclic;
            case (int)BoundaryMode.Blocking:
                return BoundaryMode.Blocking;
            default:
                return BoundaryMode.Void;
        }
    }

    internal static string[] MergeWithSystemGlobals(string[] globals) {
        var merged = new List<string>(globals);
        merged.AddRange(s_systemGlobalNames);

        return merged.ToArray();
    }
}
