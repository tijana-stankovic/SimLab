using SimLabApi;

namespace SimLab.Simulator;

internal class Position(int x, int y, int z = 0) : IPosition, IEquatable<Position> {
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Z { get; } = z;

    // IEquatable + hash allow using Position object as a key in Dictionary Simulation._cells
    public bool Equals(Position? other) {
        if (other is null) 
            return false;
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals(object? obj) {
        return obj is Position pos && Equals(pos);
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
