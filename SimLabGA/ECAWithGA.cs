// Evolving Cellular Automata with Genetic Algorithms (ECA with GA)
// Based on: https://melaniemitchell.me/PapersContent/evca-review.pdf
// Authors: Melanie Mitchell, James Crutchfield (Santa Fe Institute), Rajarshi Das (IBM Watson Research)

using SimLabApi;
using System.Globalization;

namespace SimLabGA;

public class ECAWithGA {
    private const int LayerSpacing = 50; // spacing between layers in the visualization

    // GA parameters
    private const float ElitePercent = 0.20f;
    private const float MutationProbability = 0.02f;

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
    private static int s_eliteCount; // number of elite rules

    // this is the set of CA Wolfram rules we are evolving, one per layer. 
    private static int[] s_rulesByLayer = []; // Wolfram rule number (0..255) for each layer
    private static int[] s_childrenRulesByLayer = []; // children rules for the next cycle

    // this is the current state of all CA layers
    // we use that to vizualize effect of ECA transformations to arrays in each layer, 
    // based on the rule associated with the layer. 
    private static bool[,,] s_states = new bool[0, 0, 0]; // [layer, row, col]

    // targets: 
    // for each array in each layer, there is one target value (0 or 1)
    private static byte[,] s_targets = new byte[0, 0]; // [layer, array], target value: 0 or 1

    // layers that achieved fitness below epsilon in the current cycle
    private static HashSet<int> s_successLayers = [];

    // helper structures
    private static PriorityQueue<GaCandidate, float> s_eliteQueue = new();
    private static List<GaCandidate> s_eliteParents = [];
    private static HashSet<int> s_eliteRules = [];
    private struct GaCandidate {
        public int Rule;
        public float Fitness;
    }


    // initialization phase: 
    // - read GA config, 
    // - create initial set of rules
    // - initialize arrays content for each layer
    // - create visualization cells to show initial state of arrays in each layer.
    public static void Initialization(ISimLabApi api) {
        Console.WriteLine( "    [Plug-in] ECA with GA simulation");
        api.Debug("    [Plug-in] ECA with GA initialization...");

        // read initialization parameters
        string[] initializationParameters = ReadParameters("initialization", api);
        if (initializationParameters.Length == 0) {
            Console.WriteLine("    [Plug-in] No GA configuration file specified.");
            return;
        }

        // parse GA configuration from the file specified as the Initialization phase parameter in configuration file. 
        string configFilePath = initializationParameters[0];
        ParseGaConfiguration(configFilePath);
        api.Debug($"    [Plug-in] GA config loaded. R={s_rules}, N={s_arrays}, W={s_width}, S={s_steps}, Epsilon={s_epsilon}");

        // calculate elite count based on the number of rules and elite percent
        s_eliteCount = (int)MathF.Ceiling(s_rules * ElitePercent);
        if (s_eliteCount < 1) {
            s_eliteCount = 1;
        }

        Random random = new Random(); // random generator to create initial rules and states

        // create random set of rules 
        // each rule is a number from 0 to 255 that defines the ECA transformation
        s_rulesByLayer = new int[s_rules];
        for (int layer = 0; layer < s_rules; layer++) {
            s_rulesByLayer[layer] = random.Next(0, 256); // Wolfram rule: 0..255
        }

        // all layers start with the same base state
        // then, due to the different CA rules, they will evolve differently
        InitializeRandomStates(random);

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

        Console.WriteLine( "    [Plug-in] Initialization complete");
        Console.WriteLine($"    [Plug-in] Initial visualization cells created: {createdCells + s_rules}.");
    }

    // transform visualization cells into GA cells that will be evolved by GA in the next simulation phases
    public static void PreCycle(ISimLabApi api) {
        api.Debug("    [Plug-in] ECA with GA precycle...");

        RestoreState(api);

        // rules are always loaded from marker cells
        // in that way, resume works even for all-zero layers
        LoadRulesFromMarkerCells(api);

        if (api.Cycle == 0) {
            // first cycle after initialization:
            // read currently visualized state into s_states
            LoadStatesFromVisualization(api);
        } else {
            // next cycles:
            // generate new random states
            Random random = new Random();
            InitializeRandomStates(random);
        }

        // calculate target value for each array in each layer
        CalculateTargets();

        s_childrenRulesByLayer = [];
        s_eliteQueue = new PriorityQueue<GaCandidate, float>();
        s_eliteParents = [];
        s_eliteRules = [];
        s_successLayers = [];

        // remove visualization and marker cells
        RemoveAllCells(api);
        // create REAL GA cells that will be evolved by GA
        CreateGACells(api);
    }

    public static void ProcessWorld(ISimLabApi api) {
        api.Debug("    [Plug-in] ECA with GA processworld...");
    }

    // per-cell simulation phase
    // for the current cell, ECA transformations are applied to arrays in the layer associated with that cell
    // transformations are based on the rule associated with the cell, for a specified number of steps.
    public static void Update(ISimLabApi api) {
        api.Debug("    [Plug-in] ECA with GA update...");

        ICellHandle? currentCellHandle = api.GetCurrentCell();
        if (currentCellHandle == null) {
            throw new Exception("Current GA cell is not set in Update phase.");
        }

        int layer = (int)currentCellHandle.Cell["layer"];
        int rule = (int)currentCellHandle.Cell["rule"];

        if (layer < 0 || layer >= s_rules) {
            throw new Exception($"Invalid layer value '{layer}' in Update.");
        }

        if (rule < 0 || rule > 255) {
            throw new Exception($"Invalid rule value '{rule}' in Update.");
        }

        // apply ECA transformations for the specified number of steps
        for (int step = 0; step < s_steps; step++) { 
            bool[,] nextLayerState = new bool[s_arrays, s_width];

            // in each step, pass through all arrays in the layer
            for (int row = 0; row < s_arrays; row++) {
                // and apply ECA transformation to each cell in the array
                for (int col = 0; col < s_width; col++) {
                    bool left = col > 0 && s_states[layer, row, col - 1];
                    bool center = s_states[layer, row, col];
                    bool right = col < s_width - 1 && s_states[layer, row, col + 1];

                    int leftBit = left ? 1 : 0;
                    int centerBit = center ? 1 : 0;
                    int rightBit = right ? 1 : 0;

                    int pattern = (leftBit << 2) | (centerBit << 1) | rightBit;
                    bool nextIsOne = ((rule >> pattern) & 1) == 1;

                    nextLayerState[row, col] = nextIsOne;
                }
            }

            for (int row = 0; row < s_arrays; row++) {
                for (int col = 0; col < s_width; col++) {
                    s_states[layer, row, col] = nextLayerState[row, col];
                }
            }
        }
    }

    // per-cell simulation phase
    // for the current cell, calculate fitness 
    public static void Evaluation(ISimLabApi api) {
        api.Debug("    [Plug-in] ECA with GA evaluation...");

        ICellHandle? currentCellHandle = api.GetCurrentCell();
        if (currentCellHandle == null) {
            throw new Exception("Current GA cell is not set in Evaluation phase.");
        }

        int layer = (int)currentCellHandle.Cell["layer"];
        if (layer < 0 || layer >= s_rules) {
            throw new Exception($"Invalid layer value '{layer}' in Evaluation.");
        }

        float errorSum = 0;

        // for every array in the layer
        for (int row = 0; row < s_arrays; row++) {
            // calculate current density 
            // (after ECA transformations in Update phase)
            int ones = 0;
            for (int col = 0; col < s_width; col++) {
                if (s_states[layer, row, col]) {
                    ones++;
                }
            }

            float density = (float)ones / s_width;

            // finally, calculate error for the array based on the target value
            float error = s_targets[layer, row] == 1
                ? 1 - density
                : density;

            errorSum += error; // sum errors for layer
        }

        // fitness value is average error across all arrays in the layer
        float fitness = errorSum / s_arrays;
        currentCellHandle.Cell.Fitness = fitness;

        if (fitness < s_epsilon) {
            s_successLayers.Add(layer);
        }
    }

    // per-cell simulation phase
    // if the current cell has elite fitness, add it to the elite queue
    public static void Reproduction(ISimLabApi api) {
        api.Debug("    [Plug-in] ECA with GA reproduction...");

        ICellHandle? currentCellHandle = api.GetCurrentCell();
        if (currentCellHandle == null) {
            throw new Exception("Current GA cell is not set in Reproduction phase.");
        }

        int rule = (int)currentCellHandle.Cell["rule"];
        float fitness = currentCellHandle.Cell.Fitness;

        if (rule < 0 || rule > 255) {
            throw new Exception($"Invalid rule value '{rule}' in Reproduction.");
        }

        AddEliteCandidate(rule, fitness);
    }

    // per-cell simulation phase
    // remove non-elite cells from the simulation, so only elite rules survive to the next cycle
    public static void Selection(ISimLabApi api) {
        api.Debug("    [Plug-in] ECA with GA selection...");

        // since the queue has no efficient search, 
        // we build a hash set from the queue
        if (s_eliteRules.Count == 0) {
            s_eliteRules = s_eliteQueue.UnorderedItems
                .Select(item => item.Element.Rule)
                .ToHashSet();
        }

        ICellHandle? currentCellHandle = api.GetCurrentCell();
        if (currentCellHandle == null) {
            throw new Exception("Current GA cell is not set in Selection phase.");
        }

        // if cell's rule is not in the elite set, remove the cell from the simulation
        int rule = (int)currentCellHandle.Cell["rule"];
        if (!s_eliteRules.Contains(rule)) {
            api.RemoveCurrentCell();
        }
    }

    // create children rules based on elite parents and 
    // transform GA cells back into visualization cells
    public static void PostCycle(ISimLabApi api) {
        api.Debug("    [Plug-in] ECA with GA postcycle...");

        // parent rules goes to the next cycle directly
        CopyParentRulesToChildren();

        // add children rules
        FillChildrenRules();

        // remove GA remaining cells
        RemoveAllCells(api);

        // recreate marker + visualization cells
        for (int layer = 0; layer < s_rules; layer++) {
            int yLayer = layer * LayerSpacing;
            int parentRule = s_rulesByLayer[layer];
            int childRule = s_childrenRulesByLayer[layer];

            // marker cell will store children information for the next cycle
            ICellHandle? markerCell = api.AddCell(MarkerX, yLayer, MarkerZ);
            if (markerCell != null) {
                markerCell.Cell["rule"] = childRule;
                markerCell.Cell["layer"] = layer;
                // if the layer was successful in the current cycle, 
                // color the marker cell green, otherwise white
                markerCell.Cell.Color = s_successLayers.Contains(layer)
                    ? new Color(0, 255, 0)
                    : new Color(255, 255, 255);
            }

            // visualization cells will show the effect of parent rules
            for (int row = 0; row < s_arrays; row++) {
                for (int col = 0; col < s_width; col++) {
                    if (!s_states[layer, row, col]) {
                        continue;
                    }

                    ICellHandle? visualCell = api.AddCell(col, yLayer, row);
                    if (visualCell != null) {
                        visualCell.Cell["rule"] = parentRule;
                        visualCell.Cell["layer"] = layer;
                    }
                }
            }
        }

        SaveState(api);
    }

    // helper method to read parameters for a specified simulation phase.
    private static string[] ReadParameters(string simulationPhase, ISimLabApi api) {
        string[] parameters = api.GetPlugInMethodParameters(simulationPhase);

        if (parameters.Length == 0)
            api.Debug($"    [Plug-in] Simulation phase '{simulationPhase}': no parameters.");
        else
            api.Debug($"    [Plug-in] Simulation phase '{simulationPhase}' parameters: {string.Join(", ", parameters)}");

        return parameters;
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
        s_epsilon = float.Parse(valuesByKey["Epsilon"], CultureInfo.InvariantCulture);

        if (s_rules <= 0 || s_arrays <= 0 || s_width <= 0 || s_steps <= 0) {
            throw new Exception("Invalid GA configuration. R, N, W, and S must be > 0.");
        }

        if (s_epsilon < 0) {
            throw new Exception("Invalid GA configuration. Epsilon must be >= 0.");
        }
    }

    // all layers start with the same base state
    // then, due to the different CA rules, they will evolve differently
    private static void InitializeRandomStates(Random random) {
        bool[,] baseState = new bool[s_arrays, s_width];

        // initialize base state with random bits
        for (int row = 0; row < s_arrays; row++) {
            for (int col = 0; col < s_width; col++) {
                baseState[row, col] = random.Next(0, 2) == 1;
            }
        }

        s_states = new bool[s_rules, s_arrays, s_width];

        // copy base state to all layers
        for (int layer = 0; layer < s_rules; layer++) {
            for (int row = 0; row < s_arrays; row++) {
                for (int col = 0; col < s_width; col++) {
                    s_states[layer, row, col] = baseState[row, col];
                }
            }
        }
    }

    // calculate target value for each array in each layer
    // based on initial density before ECA transformations
    private static void CalculateTargets() {
        s_targets = new byte[s_rules, s_arrays];

        for (int layer = 0; layer < s_rules; layer++) {
            for (int row = 0; row < s_arrays; row++) {
                int ones = 0;
                for (int col = 0; col < s_width; col++) {
                    if (s_states[layer, row, col]) {
                        ones++;
                    }
                }

                float density = (float)ones / s_width;
                s_targets[layer, row] = density > 0.5f ? (byte)1 : (byte)0;
            }
        }
    }

    // add candidate with elite fitness to the elite queue
    private static void AddEliteCandidate(int rule, float fitness) {
        GaCandidate candidate = new() {
            Rule = rule,
            Fitness = fitness
        };

        // PriorityQueue is a min-heap, so we use negative fitness as priority 
        // to make minimal value (the best fitness) have the highest priority in the queue
        float priority = -fitness;

        // if we still have space in the elite queue
        // just add the candidate
        if (s_eliteQueue.Count < s_eliteCount) { 
            s_eliteQueue.Enqueue(candidate, priority); 
            return;
        }

        // get current worst fitness
        if (!s_eliteQueue.TryPeek(out _, out float worstPriority)) {
            return;
        }

        // if the candidate is better than the worst in the elite queue, 
        // replace the worst with the candidate
        float worstFitness = -worstPriority;
        if (fitness < worstFitness) {
            s_eliteQueue.Dequeue();
            s_eliteQueue.Enqueue(candidate, priority);
        }
    }

    // copy elite parents rules to the next generation
    private static void CopyParentRulesToChildren() {
        s_eliteParents = s_eliteQueue.UnorderedItems
            .Select(item => item.Element)
            .OrderBy(c => c.Fitness)
            .ToList();

        s_childrenRulesByLayer = new int[s_rules];
        for (int i = 0; i < s_rules; i++) {
            s_childrenRulesByLayer[i] = -1;
        }

        int eliteIndex = 0;
        foreach (GaCandidate elite in s_eliteParents) {
            if (eliteIndex >= s_rules) {
                break;
            }

            s_childrenRulesByLayer[eliteIndex] = elite.Rule;
            eliteIndex++;
        }
    }

    // create children rules based on elite parents using crossover and mutation
    private static void FillChildrenRules() {
        if (s_eliteParents.Count == 0) {
            throw new Exception("Elite parent list is empty.");
        }

        Random random = new();
        for (int layer = 0; layer < s_rules; layer++) {
            if (s_childrenRulesByLayer[layer] != -1) {
                continue;
            }

            // randomly select two parents from the elite set with replacement
            int parentA = s_eliteParents[random.Next(0, s_eliteParents.Count)].Rule;
            int parentB = s_eliteParents[random.Next(0, s_eliteParents.Count)].Rule;

            // create child rule by crossover and mutation
            int childRule = Crossover(parentA, parentB, random);
            childRule = Mutate(childRule, random);

            s_childrenRulesByLayer[layer] = childRule;
        }
    }

    // single-point crossover between two parent rules
    private static int Crossover(int parentA, int parentB, Random random) {
        int crossoverPoint = random.Next(1, 8);
        int lowerMask = (1 << crossoverPoint) - 1;
        int upperMask = 0xFF ^ lowerMask;

        return (parentA & upperMask) | (parentB & lowerMask);
    }

    // mutation by flipping bits with a certain probability
    // randomly chose a bit position and flip it with MutationProbability
    private static int Mutate(int rule, Random random) {
        int result = rule;

        // int bit = random.Next(0, 8);
        // if (random.NextDouble() < MutationProbability) {
        //     result ^= (1 << bit);
        // }

        for (int bit = 0; bit < 8; bit++) {
          if (random.NextDouble() < MutationProbability) {
            result ^= (1 << bit);
          }
        }


        return result;
    }

    // read rules for each layer from marker cells
    private static void LoadRulesFromMarkerCells(ISimLabApi api) {
        s_rulesByLayer = new int[s_rules];

        for (int layer = 0; layer < s_rules; layer++) {
            int yLayer = layer * LayerSpacing;
            ICellHandle? markerCell = api.TryGetCell(MarkerX, yLayer, MarkerZ);
            if (markerCell == null) {
                throw new Exception($"Missing marker cell for layer {layer} at position ({MarkerX},{yLayer},{MarkerZ}).");
            }

            int rule = (int)markerCell.Cell["rule"];
            if (rule < 0 || rule > 255) {
                throw new Exception($"Invalid rule value '{rule}' in marker cell for layer {layer}.");
            }

            s_rulesByLayer[layer] = rule;
        }
    }

    // read currently visualized state into s_states
    private static void LoadStatesFromVisualization(ISimLabApi api) {
        s_states = new bool[s_rules, s_arrays, s_width];

        foreach (ICellHandle cellHandle in api.GetAllCells()) {
            Position pos = cellHandle.Position;

            // skip marker cells
            if (pos.X == MarkerX && pos.Z == MarkerZ) {
                continue;
            }

            int layer = pos.Y / LayerSpacing;
            if (layer < 0 || layer >= s_rules) {
                throw new Exception($"Layer index out of range for visualization cell {pos}.");
            }

            int row = pos.Z;
            int col = pos.X;
            if (row < 0 || row >= s_arrays || col < 0 || col >= s_width) {
                throw new Exception($"Visualization cell {pos} is outside configured array bounds.");
            }

            s_states[layer, row, col] = true;
        }
    }

    // remove all cells
    private static void RemoveAllCells(ISimLabApi api) {
        foreach (ICellHandle cellHandle in api.GetAllCells()) {
            api.RemoveCell(cellHandle.Position);
        }
    }

    // create GA cells for each layer
    // this practically transforms layers into GA cells
    private static void CreateGACells(ISimLabApi api) {
        for (int layer = 0; layer < s_rules; layer++) {
            int rule = s_rulesByLayer[layer];

            ICellHandle? gaCell = api.AddCell(layer, -1, 0);
            if (gaCell == null) {
                throw new Exception($"Unable to create GA cell for layer {layer}.");
            }

            gaCell.Cell["layer"] = layer;
            gaCell.Cell["rule"] = rule;
            gaCell.Cell.Fitness = 0;
        }
    }

    // save simulation parameters in api.Globals to persist them across simulation cycles.
    private static void SaveState(ISimLabApi api) {
        api.Globals["rules"] = s_rules;
        api.Globals["arrays"] = s_arrays;
        api.Globals["width"] = s_width;
        api.Globals["steps"] = s_steps;
        api.Globals["epsilon"] = s_epsilon;
        api.Globals["elitecount"] = s_eliteCount;
    }

    // restore simulation parameters from api.Globals at the beginning of each cycle.
    private static void RestoreState(ISimLabApi api) {
        s_rules = (int)api.Globals["rules"];
        s_arrays = (int)api.Globals["arrays"];
        s_width = (int)api.Globals["width"];
        s_steps = (int)api.Globals["steps"];
        s_epsilon = (float)api.Globals["epsilon"];
        s_eliteCount = (int)api.Globals["elitecount"];
    }
}
