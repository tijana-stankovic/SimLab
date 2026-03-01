using SimLabApi;

namespace SimLab.Simulator;

internal class Cell : ICell{
    public static long ActiveWriteCycle { get; set; } = 0;
    public static bool SkipWriteAccessCheck { get; set; } = false;

    private readonly float[] _characteristicValues;
    private CellColor _color = new(0, 0, 0); // RGB

    private long WritableInCycle { get; set; }

    public ICellColor Color {
        get => _color;
        set {
            WriteAccessCheck();
            _color = ToCellColor(value);
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
        WritableInCycle = ActiveWriteCycle;
    }

    // creating a copy of a cell: 
    // Cell copy = cell.Clone();
    public Cell Clone() {
        return new Cell(this);
    }

    // access by index – fastest
    public float this[int index] {
        get => _characteristicValues[index];
        set {
            WriteAccessCheck();
            _characteristicValues[index] = value;
        }
    }

    // access by name
    public float this[string name] {
        get => _characteristicValues[Characteristics.GetIndex(name)];
        set {
            WriteAccessCheck();
            _characteristicValues[Characteristics.GetIndex(name)] = value;
        }
    }

    public void SetColor(byte r, byte g, byte b) {
        WriteAccessCheck();
        _color = new CellColor(r, g, b);
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

    private static CellColor ToCellColor(ICellColor color) {
        if (color is CellColor cellColor) {
            return cellColor;
        }

        return new CellColor(color.R, color.G, color.B);
    }
}
