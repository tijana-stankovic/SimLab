namespace SimLabApi;

public readonly record struct Color(byte R, byte G, byte B) {
    public Color WithRed(byte rNew) => new(rNew, G, B);
    public Color WithGreen(byte gNew) => new(R, gNew, B);
    public Color WithBlue(byte bNew) => new(R, G, bNew);

    public override string ToString() {
        return $"({R},{G},{B})";
    }
}
