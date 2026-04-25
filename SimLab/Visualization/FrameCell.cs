using SimLab.Simulator;
using SimColor = SimLab.Simulator.Color;

namespace SimLab.Visualization;

internal class FrameCell(Position position, SimColor color) {
    public Position Position { get; } = position;
    public SimColor Color { get; } = color;
}
