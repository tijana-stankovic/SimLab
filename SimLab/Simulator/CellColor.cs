using SimLabApi;

namespace SimLab.Simulator;

internal readonly struct CellColor(byte r, byte g, byte b) : ICellColor {
    public byte R { get; } = r;
    public byte G { get; } = g;
    public byte B { get; } = b;

    public CellColor WithRed(byte rNew) => new(rNew, G, B);
    public CellColor WithGreen(byte gNew) => new(R, gNew, B);
    public CellColor WithBlue(byte bNew) => new(R, G, bNew);

    public override string ToString() {
        return $"({R},{G},{B})";
    }
}

