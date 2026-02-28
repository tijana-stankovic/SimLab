namespace SimLabApi;

public interface ICell {
    float this[int index] { get; set; }
    float this[string name] { get; set; }
    ICellColor Color { get; set; }
    void SetColor(byte r, byte g, byte b);
    void SetRed(byte r);
    void SetGreen(byte g);
    void SetBlue(byte b);
}
