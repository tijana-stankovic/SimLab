// Evolving Cellular Automata with Genetic Algorithms (ECA with GA)
// based on: https://melaniemitchell.me/PapersContent/evca-review.pdf
// Authors: Melanie Mitchell, James Crutchfield (Santa Fe Institute), Rajarshi Das (IBM Watson Research)

using SimLabApi;

namespace SimLabGA;

public class ECAWithGA {
    private const int LayerSpacing = 10;

    // marker cells coordinates: [MarkerX, layer * LayerSpacing, MarkerZ] 
    // they store the rule number and layer index as cell properties
    // we need marker cell for the empty layers (if they exist)
    // for non-empty layers, we can use any cell in the layer to read the rule and layer information, 
    // but for empty layers, we have no cells, so we use marker cells to store that information.
    private const int MarkerX = -1;
    private const int MarkerZ = -1;

    // static GA parameters loaded from GA.txt and persisted via api.Globals
    private static int s_rules; // number of rules / layers
    private static int s_arrays; // number of arrays per layer
    private static int s_width; // array width (number of bits in one array)
    private static int s_steps; // number of ECA steps per simulation cycle
    private static float s_epsilon; // target average error threshold

    // this is the set of CA Wolfram rules we are evolving, one per layer. 
    private static int[] s_rulesByLayer = []; // Wolfram rule number (0..255) for each layer

    // this is the current state of all CA layers
    // we use that to vizualize effect of ECA transformations to arrays in each layer, 
    // based on the rule associated with the layer. 
    private static bool[,,] s_states = new bool[0, 0, 0]; // [layer, row, col]

    // helper method to read parameters for a specified simulation phase.
    private static string[] ReadParameters(string simulationPhase, ISimLabApi api) {
        string[] parameters = api.GetPlugInMethodParameters(simulationPhase);

        if (parameters.Length == 0)
            Console.WriteLine($"    [Plug-in] Simulation phase '{simulationPhase}': no parameters.");
        else
            Console.WriteLine($"    [Plug-in] Simulation phase '{simulationPhase}' parameters: {string.Join(", ", parameters)}");

        return parameters;
    }

    // initialization phase: 
    // - read GA config, 
    // - create initial set of rules
    // - initialize arrays content for each layer
    // - create visualization cells to show initial state of arrays in each layer.
    public static void Initialization(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA with GA initialization...");

        // read initialization parameters
        string[] initializationParameters = ReadParameters("initialization", api);
        if (initializationParameters.Length == 0) {
            Console.WriteLine("    [Plug-in] No GA configuration file specified.");
            return;
        }

        // parse GA configuration from the file specified as the Initialization phase parameter in configuration file. 
        string configFilePath = initializationParameters[0];
        ParseGaConfiguration(configFilePath);
        Console.WriteLine($"    [Plug-in] GA config loaded. R={s_rules}, N={s_arrays}, W={s_width}, S={s_steps}, Epsilon={s_epsilon}");

        Random random = new Random(); // random generator to create initial rules and states

        // create random set of rules 
        // each rule is a number from 0 to 255 that defines the ECA transformation
        s_rulesByLayer = new int[s_rules];
        for (int layer = 0; layer < s_rules; layer++) {
            s_rulesByLayer[layer] = random.Next(0, 256); // Wolfram rule: 0..255
        }

        // all layers start with the same random state of arrays
        // this is base state we copy to all layers
        s_states = new bool[s_rules, s_arrays, s_width];
        bool[,] baseState = new bool[s_arrays, s_width];
        for (int row = 0; row < s_arrays; row++) {
            for (int col = 0; col < s_width; col++) {
                baseState[row, col] = random.Next(0, 2) == 1;
            }
        }

        // all layers start with the same base state
        // then, due to the different CA rules, they will evolve differently
        for (int layer = 0; layer < s_rules; layer++) {
            for (int row = 0; row < s_arrays; row++) {
                for (int col = 0; col < s_width; col++) {
                    s_states[layer, row, col] = baseState[row, col];
                }
            }
        }

        // create visualization cells for the initial state of each layer
        int createdCells = 0;
        for (int layer = 0; layer < s_rules; layer++) {
            int yLayer = layer * LayerSpacing;
            int rule = s_rulesByLayer[layer];

            ICellHandle? markerCell = api.AddCell(MarkerX, yLayer, MarkerZ);
            if (markerCell != null) {
                markerCell.Cell["rule"] = rule;
                markerCell.Cell["layer"] = layer;
                markerCell.Cell.Color = new Color(255, 255, 255);
            }

            for (int row = 0; row < s_arrays; row++) {
                for (int col = 0; col < s_width; col++) {
                    if (!s_states[layer, row, col]) {
                        continue;
                    }

                    ICellHandle? newCellHandle = api.AddCell(col, yLayer, row);
                    if (newCellHandle != null) {
                        newCellHandle.Cell["rule"] = rule;
                        newCellHandle.Cell["layer"] = layer;
                        createdCells++;
                    }
                }
            }
        }

        SaveState(api);

        Console.WriteLine($"    [Plug-in] Initial visualization cells created: {createdCells}.");
    }

    public static void PreCycle(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA with GA precycle...");
        ReadParameters("precycle", api);
        RestoreState(api);
    }

    public static void ProcessWorld(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA with GA processworld...");
        ReadParameters("processworld", api);
    }

    public static void Update(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA with GA update...");
        ReadParameters("update", api);
    }

    public static void Evaluation(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA with GA evaluation...");
        ReadParameters("evaluation", api);
    }

    public static void Selection(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA with GA selection...");
        ReadParameters("selection", api);
    }

    public static void Reproduction(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA with GA reproduction...");
        ReadParameters("reproduction", api);
    }

    public static void PostCycle(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA with GA postcycle...");
        ReadParameters("postcycle", api);
        SaveState(api);
    }

    // read simulation parameters from the plugin configuration file
    private static void ParseGaConfiguration(string configFilePath) {
        string[] lines = File.ReadAllLines(configFilePath);
        List<string> dataLines = [];

        foreach (string rawLine in lines) {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) {
                continue;
            }

            dataLines.Add(line);
        }

        Dictionary<string, string> valuesByKey = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> allowedKeys = new(StringComparer.OrdinalIgnoreCase) { "R", "N", "W", "S", "Epsilon" };

        foreach (string line in dataLines) {
            int equalsIndex = line.IndexOf('=');
            if (equalsIndex < 0) {
                throw new Exception($"Invalid GA config line '{line}'. Expected format: key=value");
            }

            string key = line.Substring(0, equalsIndex).Trim();
            string value = line.Substring(equalsIndex + 1).Trim();

            if (!allowedKeys.Contains(key)) {
                throw new Exception($"Unknown GA config parameter '{key}'. Allowed: {string.Join(", ", allowedKeys)}.");
            }

            if (valuesByKey.ContainsKey(key)) {
                throw new Exception($"Duplicate GA config parameter '{key}'.");
            }

            valuesByKey[key] = value;
        }

        foreach (string allowedKey in allowedKeys) {
            if (!valuesByKey.ContainsKey(allowedKey)) {
                throw new Exception($"Missing required GA config parameter '{allowedKey}'.");
            }
        }

        s_rules = int.Parse(valuesByKey["R"]);
        s_arrays = int.Parse(valuesByKey["N"]);
        s_width = int.Parse(valuesByKey["W"]);
        s_steps = int.Parse(valuesByKey["S"]);
        s_epsilon = float.Parse(valuesByKey["Epsilon"]);

        if (s_rules <= 0 || s_arrays <= 0 || s_width <= 0 || s_steps <= 0) {
            throw new Exception("Invalid GA configuration. R, N, W, and S must be > 0.");
        }

        if (s_epsilon < 0) {
            throw new Exception("Invalid GA configuration. Epsilon must be >= 0.");
        }
    }

    // save simulation parameters in api.Globals to persist them across simulation cycles.
    private static void SaveState(ISimLabApi api) {
        api.Globals["rules"] = s_rules;
        api.Globals["arrays"] = s_arrays;
        api.Globals["width"] = s_width;
        api.Globals["steps"] = s_steps;
        api.Globals["epsilon"] = s_epsilon;
    }

    // restore simulation parameters from api.Globals at the beginning of each cycle.
    private static void RestoreState(ISimLabApi api) {
        s_rules = (int)api.Globals["rules"];
        s_arrays = (int)api.Globals["arrays"];
        s_width = (int)api.Globals["width"];
        s_steps = (int)api.Globals["steps"];
        s_epsilon = (float)api.Globals["epsilon"];
    }
}
