namespace SimLabApi;

public interface ICellHandle {
    Position Position { get; }
    ICell Cell { get; }
}
