namespace SimLab.Simulator;

internal readonly struct Color(byte r, byte g, byte b) {
    public byte R { get; } = r;
    public byte G { get; } = g;
    public byte B { get; } = b;

    public Color WithRed(byte rNew) => new(rNew, G, B);
    public Color WithGreen(byte gNew) => new(R, gNew, B);
    public Color WithBlue(byte bNew) => new(R, G, bNew);

    public override string ToString() {
        return $"({R},{G},{B})";
    }
}
