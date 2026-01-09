namespace SimLab.Simulator;

internal readonly struct Position(int x, int y, int z = 0) : IEquatable<Position> {
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Z { get; } = z;

    public bool Equals(Position other) {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals(object? obj) {
        return obj is Position p && Equals(p);
    }

    public override int GetHashCode() {
        return HashCode.Combine(X, Y, Z);
    }

    public static bool operator ==(Position left, Position right) {
        return left.Equals(right);
    }

    public static bool operator !=(Position left, Position right) {
        return !(left == right);
    }

    public override string ToString() {
        return $"({X},{Y},{Z})";
    }    
}
