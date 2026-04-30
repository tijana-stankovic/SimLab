using System.Net.NetworkInformation;
using SimLabApi;

namespace SimLabECA_2D;

public class ECA_2D {
    private static int _fromX;
    private static int _toX;
    private static int _ruleNumber;
    private static int _currentY;
    private static HashSet<int> _nextActivePositions = [];

    private static string[] ReadParameters(string simulationPhase, ISimLabApi api) {
        string[] parameters = api.GetPlugInMethodParameters(simulationPhase);

        if (parameters.Length == 0)
            Console.WriteLine($"    [Plug-in] Simulation phase '{simulationPhase}': no parameters.");
        else
            Console.WriteLine($"    [Plug-in] Simulation phase '{simulationPhase}' parameters: {string.Join(", ", parameters)}");

        return parameters;
    }

    public static void Initialization(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA initialization...");

        // set the color of the newly created cells
        api.ForegroundColor = new Color(0, 255, 255);

        _currentY = 0;
        _nextActivePositions.Clear();

        string[] initializationParameters = ReadParameters("initialization", api);
        if (initializationParameters.Length == 0) {
            Console.WriteLine("    [Plug-in] No ECA configuration file specified.");
            return;
        }

        string configFilePath = initializationParameters[0];
        string[] lines = File.ReadAllLines(configFilePath);

        Console.WriteLine("    [Plug-in] ECA configuration loaded from file '{0}'.", configFilePath);

        List<string> dataLines = [];

        foreach (string rawLine in lines) {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) {
                continue;
            }

            dataLines.Add(line);
        }

        if (dataLines.Count < 2) {
            throw new Exception("ECA config must contain at least 2 lines: interval and rule number.");
        }

        (int fromX, int toX) = ParseInterval(dataLines[0]);
        _fromX = fromX;
        _toX = toX;
        _ruleNumber = ParseRule(dataLines[1]);
        _nextActivePositions.Clear();

        HashSet<int> initialActivePositions = [];
        for (int i = 2; i < dataLines.Count; i++) {
            int x = int.Parse(dataLines[i]);
            if (x < _fromX || x > _toX) {
                throw new Exception($"Initial active position '{x}' is outside configured interval [{_fromX}, {_toX}].");
            }

            initialActivePositions.Add(x);
        }

        foreach (int x in initialActivePositions) {
            ICellHandle? newCellHandle = api.AddCell(x, 0, 0);
            if (newCellHandle != null) {
                Console.WriteLine($"    [Plug-in] New cell added at {newCellHandle.Position, -15}");
            }
        }

        SaveState(api);

        Console.WriteLine($"    [Plug-in] ECA configured. Interval=[{_fromX},{_toX}], Rule={_ruleNumber}, Num of initial 1 ={initialActivePositions.Count}");
    }

    public static void PreCycle(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA precycle...");

        RestoreState(api);

        _nextActivePositions.Clear();

        for (int x = _fromX; x <= _toX; x++) {
            // based on computeNextRow() function from https://blakecrosley.com/blog/rule-110
            int left = GetCurrentState(api, x - 1, _currentY);
            int center = GetCurrentState(api, x, _currentY);
            int right = GetCurrentState(api, x + 1, _currentY);
            int pattern = (left << 2) | (center << 1) | right;
            bool nextValueIsOne = ((_ruleNumber >> pattern) & 1) == 1;

            if (nextValueIsOne) {
                _nextActivePositions.Add(x);
            }
        }
    }

    public static void ProcessWorld(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA processing world...");

        int nextY = _currentY + 1;

        for (int x = _fromX; x <= _toX; x++) {
            bool shouldBeAlive = _nextActivePositions.Contains(x);

            if (shouldBeAlive) {
                api.AddCell(x, nextY, 0);
            }
        }

        for (int x = _fromX; x <= _toX; x++) {
            ICellHandle? cellHandle = api.TryGetCellNext(x, _currentY, 0);
            if (cellHandle != null) {
                cellHandle.Cell.Color = new Color(255, 0, 0);
            }
        }

        _currentY = nextY;

        SaveState(api);
    }

    private static (int fromX, int toX) ParseInterval(string line) {
        string[] parts = line.Split(new char[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) {
            throw new Exception($"Invalid interval line '{line}'. Expected: from,to");
        }

        int fromX = int.Parse(parts[0]);
        int toX = int.Parse(parts[1]);

        if (toX < fromX) {
            throw new Exception($"Invalid interval [{fromX}, {toX}]. Expected from <= to.");
        }

        return (fromX, toX);
    }

    private static int ParseRule(string line) {
        int rule = int.Parse(line);
        if (rule < 0 || rule > 255) {
            throw new Exception($"Invalid ECA rule '{rule}'. Expected value in [0,255].");
        }

        return rule;
    }

    private static int GetCurrentState(ISimLabApi api, int x, int y) {
        if (x < _fromX || x > _toX) {
            return 0; // fixed boundaries
        }

        return api.TryGetCell(x, y, 0) != null ? 1 : 0;
    }

    private static void SaveState(ISimLabApi api) {
        api.Globals["from"] = (float)_fromX;
        api.Globals["to"] = (float)_toX;
        api.Globals["rule"] = (float)_ruleNumber;
        api.Globals["current"] = (float)_currentY;
    }

    private static void RestoreState(ISimLabApi api) {
        _fromX = (int)api.Globals["from"];
        _toX = (int)api.Globals["to"];
        _ruleNumber = (int)api.Globals["rule"];
        _currentY = (int)api.Globals["current"];
    }
}
