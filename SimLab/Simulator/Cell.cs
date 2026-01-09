namespace SimLab.Simulator;

internal class Cell {
    private readonly float[] _characteristicValues;
    public int[] Color { get; set; } = [0, 0, 0]; // RGB

    public Cell() {
        _characteristicValues = new float[Characteristics.Count];
    }

    // copy constructor
    // creating a copy of a cell: 
    // Cell copy = new Cell(originalCell);
    public Cell(Cell other) {
        _characteristicValues = new float[other._characteristicValues.Length];
        Array.Copy(other._characteristicValues, _characteristicValues, _characteristicValues.Length);
        Color = (int[])other.Color.Clone();
    }

    // creating a copy of a cell: 
    // Cell copy = cell.Clone();
    public Cell Clone() {
        return new Cell(this);
    }

    // access by index – fastest
    public float this[int index] {
        get => _characteristicValues[index];
        set => _characteristicValues[index] = value;
    }

    // access by name
    public float this[string name] {
        get => _characteristicValues[Characteristics.GetIndex(name)];
        set => _characteristicValues[Characteristics.GetIndex(name)] = value;
    }
}
