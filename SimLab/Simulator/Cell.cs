using SimLabApi;
using ApiColor = SimLabApi.Color;
using SimColor = SimLab.Simulator.Color;

namespace SimLab.Simulator;

internal class Cell : ICell{
    public static long ActiveWriteCycle { get; set; } = 0;
    public static bool SkipWriteAccessCheck { get; set; } = false;

    private readonly float[] _characteristicValues;
    private SimColor _color = new(0, 0, 0); // RGB
    private long Id { get; set; } = -1;
    private float Fitness { get; set; } = 0;

    private long WritableInCycle { get; set; }

    public ApiColor Color {
        get => new(_color.R, _color.G, _color.B);
        set {
            WriteAccessCheck();
            _color = ToSimColor(value);
        }
    }

    public Cell() {
        _characteristicValues = new float[Characteristics.Count];
        WritableInCycle = ActiveWriteCycle;
    }

    // copy constructor
    // creating a copy of a cell: 
    // Cell copy = new Cell(originalCell);
    public Cell(Cell other) {
        _characteristicValues = (float[])other._characteristicValues.Clone();
        _color = other._color;
        Id = other.Id;
        Fitness = other.Fitness;
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
            if (IsSystemPropertyName(name)) {
                return GetSystemPropertyValue(name);
            }

            return _characteristicValues[Characteristics.GetIndex(name)];
        }
        set {
            WriteAccessCheck();

            if (IsSystemPropertyName(name)) {
                SetSystemPropertyValue(name, value);
                return;
            }

            _characteristicValues[Characteristics.GetIndex(name)] = value;
        }
    }

    public void SetColor(byte r, byte g, byte b) {
        WriteAccessCheck();
        _color = new SimColor(r, g, b);
    }

    public void SetRed(byte r) {
        WriteAccessCheck();
        _color = _color.WithRed(r);
    }

    public void SetGreen(byte g) {
        WriteAccessCheck();
        _color = _color.WithGreen(g);
    }

    public void SetBlue(byte b) {
        WriteAccessCheck();
        _color = _color.WithBlue(b);
    }

    private void WriteAccessCheck() {
        if (SkipWriteAccessCheck)
            return;

        if (WritableInCycle != ActiveWriteCycle) {
            throw new InvalidOperationException("Cannot modify a read-only cell.");
        }
    }

    internal void SetId(long id) {
        Id = id;
    }

    internal long GetId() {
        return Id;
    }

    private static bool IsSystemPropertyName(string name) {
        return name.StartsWith('_');
    }

    private float GetSystemPropertyValue(string name) {
        float value;

        switch (name.ToLowerInvariant()) {
            case "_id":
                value = Id;
                break;
            case "_fitness":
                value = Fitness;
                break;
            default:
                throw new ArgumentException($"Unknown system property '{name}'.");
        }

        return value;
    }

    private void SetSystemPropertyValue(string name, float value) {
        switch (name.ToLowerInvariant()) {
            case "_id":
                throw new InvalidOperationException("System property '_id' is read-only.");
            case "_fitness":
                Fitness = value;
                break;
            default:
                throw new ArgumentException($"Unknown system property '{name}'.");
        }
    }

    private static SimColor ToSimColor(ApiColor color) {
        return new SimColor(color.R, color.G, color.B);
    }
}
