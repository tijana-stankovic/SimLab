using SimLabApi;

namespace SimLabGOL;

public class GameOfLife {
    private static readonly (int dx, int dy)[] NeighborOffsets = [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),           (1, 0),
        (-1, 1),  (0, 1),  (1, 1)
    ];

    private static string[] _initializationParameters = [];
    private static HashSet<Position> _candidatePositions = [];

    private static string[] ReadParameters(string simulationPhase, ISimLabApi api) {
        string[] parameters = api.GetPlugInMethodParameters(simulationPhase);

        if (parameters.Length == 0)
            Console.WriteLine($"    [Plug-in] Simulation phase '{simulationPhase}': no parameters.");
        else
            Console.WriteLine($"    [Plug-in] Simulation phase '{simulationPhase}' parameters: {string.Join(", ", parameters)}");

        return parameters;
    }

    public static void Initialization(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] Game of Life initialization...");
        _initializationParameters = ReadParameters("initialization", api);

        if (_initializationParameters.Length == 0) {
            Console.WriteLine("    [Plug-in] No initialization file specified.");
            return;
        }

        string configFilePath = _initializationParameters[0];
        string[] lines = File.ReadAllLines(configFilePath);

        Console.WriteLine("    [Plug-in] Cell positions loaded from file '{0}'.", configFilePath);
        foreach (string rawLine in lines) {
            string line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            string[] coordinates = line.Split(',');
            int x = int.Parse(coordinates[0]);
            int y = int.Parse(coordinates[1]);

            ICellHandle? newCellHandle = api.AddCell(new Position(x, y, 0));
            if (newCellHandle != null) {
                Console.WriteLine($"    [Plug-in] New cell added at {newCellHandle.Position, -15}");
            }
        }
    }

    public static void PreCycle(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] Game of Life precycle...");
        _candidatePositions.Clear();

        foreach (ICellHandle cellHandle in api.GetAllCells()) {
            Position position = cellHandle.Position;
            _candidatePositions.Add(position);
            foreach (var (dx, dy) in NeighborOffsets) {
                _candidatePositions.Add(new Position(position.X + dx, position.Y + dy));
            }
        }

        Console.WriteLine($"    [Plug-in] Candidate positions prepared: {_candidatePositions.Count}");
    }

    public static void ProcessWorld(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] Game of Life processing world...");

        foreach (Position position in _candidatePositions) {
            bool isAlive = api.TryGetCell(position) != null;
            int liveNeighbors = CountLiveNeighbors(api, position);

            if (isAlive) {
                if (liveNeighbors != 2 && liveNeighbors != 3) {
                    api.RemoveCell(position);
                }
            } else if (liveNeighbors == 3) {
                api.AddCell(position);
            }
        }
    }

    private static int CountLiveNeighbors(ISimLabApi api, Position position) {
        int count = 0;

        foreach (var (dx, dy) in NeighborOffsets) {
            if (api.TryGetCell(new Position(position.X + dx, position.Y + dy)) != null) {
                count++;
            }
        }

        return count;
    }
}
