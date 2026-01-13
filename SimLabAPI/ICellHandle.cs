namespace SimLabApi;

public interface ICellHandle {
    IPosition Position { get; }
    ICell Cell { get; }
}
