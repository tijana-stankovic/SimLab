using SimLabApi;
using ApiColor = SimLabApi.Color;
using SimColor = SimLab.Simulator.Color;

namespace SimLab.Simulator;

internal class Cell : ICell{
    public static long ActiveWriteCycle { get; set; } = 0;
    public static bool SkipWriteAccessCheck { get; set; } = false;
    internal static SimColor DefaultColor { get; set; } = new(0, 0, 0);

    // CellID = unique identifier for the cell, assigned by the database when the cell is first saved
    private long Id { get; set; } = -1;

    // system cell characteristics names 
    // (starting with underscore to distinguish them from user-defined characteristics)
    internal const string IdName = "_id";
    internal const string FitnessName = "_fitness";
    internal const string ColorRName = "_color_r";
    internal const string ColorGName = "_color_g";
    internal const string ColorBName = "_color_b";

    // list of system cell characteristics names
    private static readonly string[] s_systemCharacteristicNames = [
        FitnessName,
        ColorRName,
        ColorGName,
        ColorBName
    ];

    // cell characteristics values (user plus system at the end)
    private readonly float[] _characteristicValues;

    private long WritableInCycle { get; set; }

    // Cell constructor, for creating a new cell with default values
    public Cell() {
        _characteristicValues = new float[Characteristics.Count];
        WritableInCycle = ActiveWriteCycle;
        SetColor(DefaultColor.R, DefaultColor.G, DefaultColor.B);
        this[FitnessName] = 0;
    }

    // copy constructor
    // creating a copy of a cell: 
    // Cell copy = new Cell(originalCell);
    public Cell(Cell other) {
        _characteristicValues = (float[])other._characteristicValues.Clone();
        Id = other.Id;
        WritableInCycle = ActiveWriteCycle;
    }

    // creating a copy of a cell: 
    // Cell copy = cell.Clone();
    public Cell Clone() {
        return new Cell(this);
    }

    // access by index - fastest
    public float this[int index] {
        get => _characteristicValues[index];
        set {
            WriteAccessCheck();
            _characteristicValues[index] = value;
        }
    }

    // access by name
    public float this[string name] {
        get {
            // read "_id" system cell characteristic from the Id property, 
            // not from the characteristic values array
            if (name.Equals(IdName, StringComparison.OrdinalIgnoreCase)) {
                return Id;
            }

            return _characteristicValues[Characteristics.GetIndex(name)];
        }
        set {
            WriteAccessCheck();

            // prevent writing to "_id" system cell characteristic
            if (name.Equals(IdName, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("System characteristic '_id' is read-only.");
            }

            _characteristicValues[Characteristics.GetIndex(name)] = value;
        }
    }

    // Id setter
    internal void SetId(long id) {
        Id = id;
    }

    // Id getter
    internal long GetId() {
        return Id;
    }

    // ApiColor property getter and setter
    // (for using from plugins)
    public ApiColor Color {
        get {
            SimColor color = GetColor();
            return new ApiColor(color.R, color.G, color.B);
        }
        set => SetColor(value.R, value.G, value.B);
    }

    public float Fitness {
        get => this[FitnessName];
        set => this[FitnessName] = value;
    }

    // SimColor getter using system cell characteristics
    internal SimColor GetColor() {
        return new SimColor((byte)this[ColorRName], (byte)this[ColorGName], (byte)this[ColorBName]);
    }

    // SimColor setter using system cell characteristics
    public void SetColor(byte r, byte g, byte b) {
        WriteAccessCheck();
        this[ColorRName] = r;
        this[ColorGName] = g;
        this[ColorBName] = b;
    }

    public void SetRed(byte r) {
        WriteAccessCheck();
        this[ColorRName] = r;
    }

    public void SetGreen(byte g) {
        WriteAccessCheck();
        this[ColorGName] = g;
    }

    public void SetBlue(byte b) {
        WriteAccessCheck();
        this[ColorBName] = b;
    }

    private void WriteAccessCheck() {
        if (SkipWriteAccessCheck)
            return;

        if (WritableInCycle != ActiveWriteCycle) {
            throw new InvalidOperationException("Cannot modify a read-only cell.");
        }
    }

    internal static string[] MergeWithSystemCharacteristics(string[] characteristics) {
        var merged = new List<string>(characteristics);
        merged.AddRange(s_systemCharacteristicNames);

        return merged.ToArray();
    }
}
