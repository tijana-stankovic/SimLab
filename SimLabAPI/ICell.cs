namespace SimLabApi;

public interface ICell {
    float this[int index] { get; set; }
    float this[string name] { get; set; }
    int[] Color { get; set; }
}
