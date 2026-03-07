namespace SimLabApi;

public readonly record struct Position(int X, int Y, int Z = 0) {
    public override string ToString() {
        return $"({X},{Y},{Z})";
    }
}
