namespace SimLabApi;

public interface IGlobals {
    float this[int index] { get; set; }
    float this[string name] { get; set; }
}
