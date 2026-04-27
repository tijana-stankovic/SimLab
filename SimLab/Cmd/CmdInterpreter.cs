using SimLab.ApiImplementation;
using SimLab.Configuration;
using SimLab.Simulator;
using SimLab.Status;
using SimLab.Output;
using SimLab.PlugInUtility;
using System.Reflection;
using SimLab.Visualization;
using SimLab.DB;
using System.Text;
using SimWorld = SimLab.Simulator.World;

namespace SimLab.Cmd;

/// <summary>
/// This is the Command Interpreter - the main processing class of the Controller.
/// It executes all commands of the SimLab application and is responsible for communication with other parts.
/// </summary>
internal class CmdInterpreter {
    public StatusCode StatusCode { get; set; } = StatusCode.NoError;
    public bool QuitSignal { get; set; } = false;

    public Simulation? Simulation { get; set; } = null;
    private FrameBuffer? FrameBuffer { get; set; } = null;
    private Visualizer Visualizer { get; } = new();
    private IDatabase? Database { get; set; }
    private const string DatabaseConfigFileName = "DatabaseConfig.json";

    public CmdInterpreter(string[] args) {
        if (!InitializeDatabase()) {
            QuitSignal = true;
            return;
        }

        if (args.Length != 0) {
            WorldAdd(args[0]);
        }
    }

    /// <summary>
    /// Entry point for command processing. Executes the specified command.
    /// </summary>
    /// <param name="cmd">The command to execute.</param>
    public void ExecuteCommand(Command cmd) {
        StatusCode = StatusCode.NoError;

        string command = cmd.Name.ToUpperInvariant();

        switch (command) {
            case "": // do nothing
                break;

            case "H":
            case "HELP":
                Help();
                break;

            case "AB":
            case "ABOUT":
                About();
                break;

            case "E":
            case "X":
            case "EXIT":
                Exit();
                break;

            case "W":
            case "WORLD":
                World(cmd.Args);
                break;

            case "SIM":
            case "SIMULATION":
                SimulationCommand(cmd.Args);
                break;

            case "S":
            case "SHOW":
                Show(cmd.Args);
                break;

            default:
                StatusCode = StatusCode.UnknownCommand;
                View.PrintStatus(StatusCode);
                break;
        }
    }

    /// <summary>
    /// HELP command entry point.
    /// Displays the help information (short description) for each command.
    /// </summary>
    static private void Help() {
        View.Print("List of available commands:");
        View.Print("- HELP (H)");
        View.Print("  Display page with list of commands.");
        View.Print("- ABOUT (AB)");
        View.Print("  Display information about program.");
        View.Print("- EXIT (E, X)");
        View.Print("  Exit the program.");
        View.Print("- WORLD (W) <subcommand> [arguments]");
        View.Print("  Manage worlds in database.");
        View.Print("  Subcommands:");
        View.Print("    - WORLD LIST");
        View.Print("    - WORLD ADD <json-config-file>");
        View.Print("    - WORLD LOAD <world-uid>");
        View.Print("    - WORLD REMOVE <world-uid>");
        View.Print("- SIMULATION (SIM) <subcommand> [arguments]");
        View.Print("  Run/control simulation.");
        View.Print("  Subcommands:");
        View.Print("    - SIMULATION NEXT [<number-of-cycles>]");
        View.Print("    - SIMULATION SHOW [<frame-number>]");
        View.Print("    - SIMULATION INIT");
        View.Print("- SHOW (S) [<frame-number>]");
        View.Print("  Open the visualization window for generated frames.");
        View.Print("- TEST (T)");
        View.Print("  Test a plug-in method.");
    }

    /// <summary>
    /// ABOUT command entry point. 
    /// Displays information about the program.
    /// </summary>
    static private void About() {
        View.FullProgramInfo();
    }

    /// <summary>
    /// EXIT command entry point.
    /// Sets the request (quit signal) for exiting the program.
    /// </summary>
    private void Exit() {
        Database?.Disconnect();
        QuitSignal = true;
    }

    static private MethodInfo? GetMethod(string plugInMethodPath) {
        MethodInfo? pluginMethod = null;

        if (PlugIn.ParseMethodPath(plugInMethodPath,
            out string dllName, 
            out string className, 
            out string methodName, 
            out string? error)) {

            pluginMethod = PlugIn.GetMethod(dllName, className, methodName, out error);

            if (pluginMethod == null) {
                View.Print($"Error retrieving plug-in method: '{methodName}' from class '{className}' in DLL '{dllName}'.");
                View.Print($"Error text:\n{error}");
            }
        } else {
            View.Print($"Error parsing plug-in method path: '{plugInMethodPath}'");
            View.Print($"Error text:\n{error}");
        }
        
        return pluginMethod;
    }

    static private SimulationMode ParseModeOrDefault(string? modeText, out bool wasInvalid) {
        wasInvalid = false;
        if (string.IsNullOrWhiteSpace(modeText)) {
            return SimulationMode.SynchronousCA;
        }

        switch (modeText.Trim().ToUpperInvariant()) {
            case "SYNCHRONOUSCA":
                return SimulationMode.SynchronousCA;
            case "ASYNCHRONOUS":
                return SimulationMode.Asynchronous;
            default:
                wasInvalid = true;
                return SimulationMode.SynchronousCA;
        }
    }

    private bool InitializeDatabase() {
        View.Print($"[Info] Loading database configuration from JSON file: {DatabaseConfigFileName}");

        if (!DatabaseConfigJson.LoadConfiguration(DatabaseConfigFileName, out DatabaseCfg? config, out string? configError)) {
            View.Print($"[Fatal] {configError}");
            View.Print("[Fatal] Program cannot continue without a valid database configuration.");
            return false;
        }

        if (config == null) {
            return false;
        }

        if (!ParseDatabaseType(config.Type!, out DatabaseType databaseType, out string? parseError)) {
            View.Print($"[Fatal] {parseError}");
            View.Print("[Fatal] Program cannot continue.");
            return false;
        }

        string password = Cli.ReadPassword($"Enter database password for user '{config.User}': ");
        string connectionString = BuildConnectionString(config, password);

        IDatabase database;
        try {
            database = DatabaseSelector.Select(databaseType, connectionString);
        } catch (Exception ex) {
            View.Print($"[Fatal] Unable to connect to database: {ex.Message}");
            View.Print("[Fatal] Program cannot continue.");
            return false;
        }

        if (!database.Connect(out string? connectError)) {
            database.Disconnect();
            View.Print($"[Fatal] Database connection failed: {connectError}");
            View.Print("[Fatal] Program cannot continue.");
            return false;
        }

        Database = database;

        if (Database.GetConnectionInfo(out string? userName, out string? databaseName, out string? infoError)) {
            View.Print($"[Info] Connected to database '{databaseName}' as user '{userName}'.");
        } else {
            View.Print($"[Warning] Connected, but could not read connection info: {infoError}");
        }

        return true;
    }

    static private bool ParseDatabaseType(string databaseTypeText, out DatabaseType databaseType, out string? error) {
        if (string.IsNullOrWhiteSpace(databaseTypeText)) {
            databaseType = default;
            error = "Database type is empty in DatabaseConfig.json.";
            return false;
        }

        switch (databaseTypeText.Trim().ToUpperInvariant()) {
            case "POSTGRESQL":
                databaseType = DatabaseType.PostgreSql;
                error = null;
                return true;
            case "ORACLE":
                databaseType = DatabaseType.Oracle;
                error = null;
                return true;
            case "SQLSERVER":
                databaseType = DatabaseType.SqlServer;
                error = null;
                return true;
            default:
                databaseType = default;
                error = $"Unsupported database type '{databaseTypeText}' in DatabaseConfig.json.";
                return false;
        }
    }

    static private string BuildConnectionString(DatabaseCfg config, string password) {
        var builder = new StringBuilder();
        builder.Append("Host=").Append(config.Host).Append(';');
        builder.Append("Port=").Append(config.Port).Append(';');
        builder.Append("Database=").Append(config.Database).Append(';');
        builder.Append("Username=").Append(config.User).Append(';');
        builder.Append("Password=").Append(password).Append(';');

        return builder.ToString();
    }

    // Add system cell characteristics to the user cell characteristics defined in the world configuration
    static private WorldCfg ExtendUserCellCharacteristics(WorldCfg worldCfg) {
        return new WorldCfg {
            Uid = worldCfg.Uid,
            Name = worldCfg.Name,
            Space = worldCfg.Space,
            Dimensions = worldCfg.Dimensions,
            Characteristics = Cell.MergeWithSystemCharacteristics(worldCfg.Characteristics),
            Globals = SimWorld.MergeWithSystemGlobals(worldCfg.Globals),
            Mode = worldCfg.Mode,
            Foreground = worldCfg.Foreground,
            Background = worldCfg.Background,
            Initialization = worldCfg.Initialization,
            PreCycle = worldCfg.PreCycle,
            ProcessWorld = worldCfg.ProcessWorld,
            Update = worldCfg.Update,
            Evaluation = worldCfg.Evaluation,
            Reproduction = worldCfg.Reproduction,
            Selection = worldCfg.Selection,
            PostCycle = worldCfg.PostCycle
        };
    }

    private void ApplyWorldConfiguration(WorldCfg worldCfg, string sourceDescription) {
        Characteristics.Init(worldCfg.Characteristics);
        Globals.Init(worldCfg.Globals);
        Simulation = new Simulation(new World(worldCfg));
        FrameBuffer = new FrameBuffer(Simulation.World);
        Simulation.Mode = ParseModeOrDefault(worldCfg.Mode, out bool invalidMode);
        if (invalidMode) {
            View.Print($"[Warning] Unknown simulation mode '{worldCfg.Mode}'. Using '{Simulation.Mode}'.");
        }

        View.Print($"[Info] Configuration successfully loaded from {sourceDescription}.");
        View.Print($"[Info] Simulation mode: {Simulation.Mode}");

        if (worldCfg.Initialization != null) {
            Simulation.InitializationMethod = GetMethod(worldCfg.Initialization.Method);
            Simulation.InitializationParameters = worldCfg.Initialization.Parameters;
        }
        if (worldCfg.PreCycle != null) {
            Simulation.PreCycleMethod = GetMethod(worldCfg.PreCycle.Method);
            Simulation.PreCycleParameters = worldCfg.PreCycle.Parameters;
        }
        if (worldCfg.ProcessWorld != null) {
            Simulation.ProcessWorldMethod = GetMethod(worldCfg.ProcessWorld.Method);
            Simulation.ProcessWorldParameters = worldCfg.ProcessWorld.Parameters;
        }
        if (worldCfg.Update != null) {
            Simulation.UpdateMethod = GetMethod(worldCfg.Update.Method);
            Simulation.UpdateParameters = worldCfg.Update.Parameters;
        }
        if (worldCfg.Evaluation != null) {
            Simulation.EvaluationMethod = GetMethod(worldCfg.Evaluation.Method);
            Simulation.EvaluationParameters = worldCfg.Evaluation.Parameters;
        }
        if (worldCfg.Reproduction != null) {
            Simulation.ReproductionMethod = GetMethod(worldCfg.Reproduction.Method);
            Simulation.ReproductionParameters = worldCfg.Reproduction.Parameters;
        }
        if (worldCfg.Selection != null) {
            Simulation.SelectionMethod = GetMethod(worldCfg.Selection.Method);
            Simulation.SelectionParameters = worldCfg.Selection.Parameters;
        }
        if (worldCfg.PostCycle != null) {
            Simulation.PostCycleMethod = GetMethod(worldCfg.PostCycle.Method);
            Simulation.PostCycleParameters = worldCfg.PostCycle.Parameters;
        }
    }

    private void SimulationCommand(string[] args) {
        if (args.Length == 0) {
            View.Print("SIMULATION command requires a subcommand.");
            View.Print("Use: SIMULATION NEXT [n] | SIMULATION SHOW [arguments-for-show] | SIMULATION INIT");
            return;
        }

        string subcommand = args[0].ToUpperInvariant();

        switch (subcommand) {
            case "NEXT":
                if (args.Length > 2) {
                    View.Print("Use: SIMULATION NEXT [n]");
                    return;
                }

                if (Simulation == null) {
                    View.Print("No simulation loaded. Use WORLD ADD or WORLD LOAD first.");
                    return;
                }

                int numberOfCycles;
                if (args.Length == 2) {
                    if (!int.TryParse(args[1], out numberOfCycles) || numberOfCycles < 0) {
                        View.Print("Parameter n must be a non-negative integer.");
                        return;
                    }
                } else {
                    numberOfCycles = Simulation.IsRunning ? 1 : 0;
                }

                SimulationRun(numberOfCycles);
                break;

            case "SHOW":
                Show(args.Skip(1).ToArray());
                break;

            case "INIT":
                if (args.Length != 1) {
                    View.Print("Use: SIMULATION INIT");
                    return;
                }
                SimulationInit();
                break;

            default:
                View.Print($"Unknown SIMULATION subcommand: {args[0]}");
                View.Print("Use: SIMULATION NEXT [n] | SIMULATION SHOW [arguments-for-show] | SIMULATION INIT");
                break;
        }
    }

    private void SimulationInit() {
        if (Simulation == null) {
            View.Print("No simulation loaded. Use WORLD ADD or WORLD LOAD first.");
            return;
        }

        if (!Simulation.IsRunning) {
            SimulationRun(0);
            return;
        }

        char answer = Cli.AskYesNo("The simulation has already been started. Do you want to delete and reinitialize it?", false);
        if (answer == 'N') {
            View.Print("[Simulation INIT] Operation canceled.");
            return;
        }

        if (Database == null) {
            View.Print("[Simulation INIT] Database is not initialized.");
            return;
        }

        if (!Simulation.World.Id.HasValue) {
            View.Print("[Simulation INIT] Active world ID is missing.");
            return;
        }

        if (!Database.ResetWorldSimulation(Simulation.World.Id.Value, out string? resetError)) {
            View.Print($"[Simulation INIT] Failed to reset simulation in database: {resetError}");
            return;
        }

        if (string.IsNullOrWhiteSpace(Simulation.World.Uid)) {
            View.Print("[Simulation INIT] Active world UID is missing.");
            return;
        }

        string worldUid = Simulation.World.Uid;
        if (!WorldLoad(worldUid)) {
            View.Print("[Simulation INIT] Failed to load world after reset.");
            return;
        }

        SimulationRun(0);
    }

    private void SimulationRun(int numberOfCycles) {
        if (Simulation == null) {
            View.Print("No simulation loaded. Use WORLD ADD or WORLD LOAD first.");
            return;
        }

        View.Print(""); View.Print(""); View.Print("");

        Simulation sim = Simulation;
        var api = new API(sim);

        // Initialization
        if (!sim.IsRunning) { 
            sim.IsRunning = true;
            sim.BeginCycle();
            
            if (sim.InitializationMethod != null) {
                View.Print("[Simulation] Calling plug-in initialization method.");
                if (PlugIn.Execute(sim.InitializationMethod, api, out var error))
                    View.Print("[Simulation] Initialization completed successfully.");
                else
                    View.Print($"[Simulation] Initialization error: {error}");
            }
            sim.EndCycle();
            FrameBuffer?.Capture(sim);
            if (Database != null) {
                if (!Database.SaveCurrentState(sim, out string? saveError)) {
                    View.Print($"[Simulation] Failed to save cycle {sim.Cycle} to database: {saveError}");
                }
            }
            PrintCellCharacteristics(sim, "Initial characteristics of all cells:");
        } else {
            PrintCellCharacteristics(sim, "Current characteristics of all cells:");
        }


        // Main simulation loop
        for (int i = 0; i < numberOfCycles; i++) {
            sim.Cycle++;
            View.Print($"\n[Simulation] Starting cycle {sim.Cycle}.");
            sim.BeginCycle();

            void ExecuteIfNotNull(MethodInfo? method) {
                if (method != null) {
                    if (PlugIn.Execute(method, api, out var error)) 
                        View.Print($"    [Simulation] Plug-in method {method.Name} executed successfully.");
                    else 
                        View.Print($"    [Simulation] Error executing plug-in method {method.Name}: {error}");
                }
            }

            void ExecutePerCellIfNotNull(MethodInfo? method) {
                if (method == null) {
                    return;
                }

                IEnumerable<CellHandle> cellHandles = sim.Mode == SimulationMode.SynchronousCA
                    ? sim.GetAllCells()
                    : sim.GetAllCells().ToList();

                foreach (var cellHandle in cellHandles) {
                    if (sim.Mode == SimulationMode.SynchronousCA) {
                        sim.SetCurrentCell(cellHandle.Position);
                    } else {
                        sim.SetCurrentCell(cellHandle);
                    }

                    if (sim.GetCurrentCell() == null) {
                        continue;
                    }

                    ExecuteIfNotNull(method);
                }

                sim.ClearCurrentCell();
            }

            ExecuteIfNotNull(sim.PreCycleMethod);
            ExecuteIfNotNull(sim.ProcessWorldMethod);
            ExecutePerCellIfNotNull(sim.UpdateMethod);
            ExecutePerCellIfNotNull(sim.EvaluationMethod);
            ExecutePerCellIfNotNull(sim.ReproductionMethod);
            ExecutePerCellIfNotNull(sim.SelectionMethod);
            ExecuteIfNotNull(sim.PostCycleMethod);
            sim.EndCycle();
            FrameBuffer?.Capture(sim);
            if (Database != null) {
                if (!Database.SaveCurrentState(sim, out string? saveError)) {
                    View.Print($"[Simulation] Failed to save cycle {sim.Cycle} to database: {saveError}");
                }
            }

            PrintCellCharacteristics(sim, "Characteristics of all cells after cycle " + sim.Cycle + ":");
        }

        View.Print(""); View.Print(""); View.Print("");
    }

    // go through all cells and print their characteristics
    static private void PrintCellCharacteristics(Simulation sim, string header) {
        View.Print($"\n[Simulation] {header}");
        foreach (var cellHandle in sim.GetAllCells()) {
            View.Print($"    @ {cellHandle.Position, -15}", false);
            View.Print($"id: {cellHandle.Cell["_id"], -10}", false);
            // print all cell characteristics
            foreach (var characteristic in Characteristics.GetNames().Select(name => (Name: name, Value: cellHandle.Cell[name]))) {
                View.Print($"{characteristic.Name}: {characteristic.Value, -6}", false);
            }
            View.Print("");
        }
    }

    private void Show(string[] args) {
        if (args.Length != 0) {
            View.Print("Argument support is not implemented yet.");
            return;
        }

        if (FrameBuffer == null || !FrameBuffer.HasFrames) {
            View.Print("[Show] No generated frames available. Run SIMULATION NEXT first.");
            return;
        }

        try {
            //int startFrameIndex = FrameBuffer.GetStartFrameIndex();
            int lastViewedFrameIndex = FrameBuffer.GetLastViewedFrameIndex();
            int frameIndex = Visualizer.Show(FrameBuffer);
            if (frameIndex == 0) {
                View.Print($"[Show] Closed visualization on the initial cells position.");
            } else if (frameIndex > 0) {
                View.Print($"[Show] Closed visualization on frame {frameIndex}/{FrameBuffer.Count - 1}.");
            }

            if (frameIndex >= 0 && frameIndex != lastViewedFrameIndex && Database != null && Simulation != null && Simulation.World.Id.HasValue) {
                if (Database.UpdateWorldLastViewedFrame(Simulation.World.Id.Value, frameIndex, out string? updateError)) {
                    Simulation.World.LastViewedFrame = frameIndex;
                } else {
                    View.Print($"[Show] Failed to update last viewed frame in database: {updateError}");
                }
            }
        } catch (Exception ex) {
            View.Print($"[Show] Visualization error: {ex.Message}");
        } finally {
            UpdateCellDefaultColor();
        }
    }

    private void World(string[] args) {
        if (args.Length == 0) {
            View.Print("WORLD command requires a subcommand.");
            View.Print("Use: WORLD LIST | WORLD ADD <json-config-file> | WORLD LOAD <world-uid> | WORLD REMOVE <world-uid>");
            return;
        }

        string subcommand = args[0].ToUpperInvariant();

        switch (subcommand) {
            case "LIST":
                if (args.Length != 1) {
                    View.Print("WORLD LIST does not accept additional arguments.");
                    return;
                }
                WorldList();
                break;

            case "ADD":
                if (args.Length != 2) {
                    View.Print("Use: WORLD ADD <json-config-file>");
                    return;
                }
                WorldAdd(args[1]);
                break;

            case "LOAD":
                if (args.Length != 2) {
                    View.Print("Use: WORLD LOAD <world-uid>");
                    return;
                }
                WorldLoad(args[1]);
                break;

            case "REMOVE":
                if (args.Length != 2) {
                    View.Print("Use: WORLD REMOVE <world-uid>");
                    return;
                }
                WorldRemove(args[1]);
                break;

            default:
                View.Print($"Unknown WORLD subcommand: {args[0]}");
                View.Print("Use: WORLD LIST | WORLD ADD <json-config-file> | WORLD LOAD <world-uid> | WORLD REMOVE <world-uid>");
                break;
        }
    }

    private void WorldList() {
        if (Database == null) {
            View.Print("[WORLD LIST] Database is not initialized.");
            return;
        }

        if (!Database.ListWorlds(out List<DbWorldInfo> worlds, out string? error)) {
            View.Print($"[WORLD LIST] Error: {error}");
            return;
        }

        if (worlds.Count == 0) {
            View.Print("[WORLD LIST] No worlds found in database.");
            return;
        }

        View.Print("[WORLD LIST] Existing worlds:");
        View.Print("id      uid/name                        space    x      y      z      mode  last_cycle  next_cell_id  last_viewed_cycle");

        foreach (DbWorldInfo worldInfo in worlds) {
            string lastCycleText = worldInfo.LastCycle.HasValue ? worldInfo.LastCycle.Value.ToString() : "n/a";
            if (lastCycleText == "0") {
                lastCycleText = "init.pos.";
            }
            string lastViewedFrameText = worldInfo.LastViewedFrame.HasValue ? worldInfo.LastViewedFrame.Value.ToString() : "n/a";
            if (lastViewedFrameText == "0") {
                lastViewedFrameText = "init.pos.";
            }

            View.Print($"{worldInfo.Id,-8}{worldInfo.Uid,-32}{worldInfo.Space,-9}{worldInfo.X,-7}{worldInfo.Y,-7}{worldInfo.Z,-7}{worldInfo.Mode,-6}{lastCycleText,-12}{worldInfo.NextCellId,-14}{lastViewedFrameText}");
            View.Print($"{string.Empty,-8}{worldInfo.Name}");
        }
    }

    private void WorldAdd(string jsonConfigFile) {
        if (Database == null) {
            View.Print("[WORLD ADD] Database is not initialized.");
            return;
        }

        if (!ConfigJson.LoadConfiguration(jsonConfigFile, out WorldCfg? worldCfg)) {
            View.Print($"[WORLD ADD] Error loading configuration from '{jsonConfigFile}'.");
            return;
        }

        if (worldCfg == null) {
            View.Print($"[WORLD ADD] Configuration is empty in '{jsonConfigFile}'.");
            return;
        }

        // add system cell characteristics to the user cell characteristics defined in the world configuration
        worldCfg = ExtendUserCellCharacteristics(worldCfg);

        if (!Database.AddWorldDefinition(worldCfg, out int worldId, out string? worldUid, out string? error)) {
            View.Print($"[WORLD ADD] Error: {error}");
            return;
        }

        View.Print($"[WORLD ADD] World '{worldUid}' added successfully (id={worldId}).");

        // after successful add, activate same world config in memory.
        ApplyWorldConfiguration(worldCfg, "JSON file");
        if (Simulation != null) {
            Simulation.World.Id = worldId;
            Simulation.World.Uid = worldUid;
            Simulation.World.LastCycle = null;
            Simulation.World.NextCellId = 1;
            Simulation.World.LastViewedFrame = null;
        }
    }

    private bool WorldLoad(string worldUid) {
        if (Database == null) {
            View.Print("[WORLD LOAD] Database is not initialized.");
            return false;
        }

        if (!Database.LoadWorldDefinition(
            worldUid,
            out int worldId,
            out WorldCfg? worldCfg,
            out long? lastCycle,
            out long nextCellId,
            out long? lastViewedFrame,
            out string? error)) {
            View.Print($"[WORLD LOAD] Error: {error}");
            return false;
        }

        if (worldCfg == null) {
            View.Print($"[WORLD LOAD] No world definition returned for UID '{worldUid}'.");
            return false;
        }

        ApplyWorldConfiguration(worldCfg, "database");
        try {
            if (Simulation != null) {
                Simulation.World.Id = worldId;
                Simulation.World.Uid = worldCfg.Uid;
                Simulation.World.LastCycle = lastCycle;
                Simulation.World.NextCellId = nextCellId;
                Simulation.World.LastViewedFrame = lastViewedFrame;

                if (lastCycle != null) {
                    long cycleNumber = (long)lastCycle;
                    if (!Database.LoadState(worldId, cycleNumber, Simulation, out string? loadStateError)) {
                        View.Print($"[WORLD LOAD] World definition loaded, but failed to restore last state: {loadStateError}");
                        return false;
                    }

                    if (!BuildFrameBufferFromDatabaseHistory(worldId, cycleNumber, lastViewedFrame, nextCellId, worldCfg, out string? loadHistoryError)) {
                        FrameBuffer = null;
                        View.Print($"[WORLD LOAD] Warning: last state restored, but failed to build visualization frame history: {loadHistoryError}");
                    }
                }
            }

            View.Print($"[WORLD LOAD] World '{worldCfg.Uid}' loaded successfully (id={worldId}).");
            return true;
        } finally {
            UpdateCellDefaultColor();
        }
    }

    // load all saved cycles (0..lastCycle) into memory and build full frame buffer
    private bool BuildFrameBufferFromDatabaseHistory(int worldId, long lastCycle, long? lastViewedFrame, long nextCellId, WorldCfg worldCfg, out string? error) {
        if (Database == null) {
            error = "Database is not initialized.";
            return false;
        }

        // use a separate helper simulation object so visualization history loading
        // does not affect the active/current simulation state
        var helperSimulation = new Simulation(new World(worldCfg));
        helperSimulation.World.Id = worldId;
        helperSimulation.World.Uid = worldCfg.Uid;
        helperSimulation.World.NextCellId = nextCellId;
        helperSimulation.Mode = ParseModeOrDefault(worldCfg.Mode, out _);

        try {
            FrameBuffer = new FrameBuffer(helperSimulation.World);

            for (long cycleNumber = 0; cycleNumber <= lastCycle; cycleNumber++) {
                if (!Database.LoadState(worldId, cycleNumber, helperSimulation, out string? loadStateError)) {
                    error = $"Failed to load cycle {cycleNumber}: {loadStateError}";
                    return false;
                }

                FrameBuffer.Capture(helperSimulation);
            }

            if (lastViewedFrame != null) {
                long viewedFrame = (long)lastViewedFrame;
                int lastViewedFrameIndex = viewedFrame > int.MaxValue
                    ? int.MaxValue
                    : (int)viewedFrame;
                FrameBuffer.SetLastViewedFrameIndex(lastViewedFrameIndex);
            }

            error = null;
            return true;
        } finally {
            UpdateCellDefaultColor();
        }
    }

    private void UpdateCellDefaultColor() {
        if (Simulation != null) {
            Cell.DefaultColor = Simulation.World.ForegroundColor;
        }
    }

    private void WorldRemove(string worldUid) {
        if (Database == null) {
            View.Print("[WORLD REMOVE] Database is not initialized.");
            return;
        }

        if (!Database.RemoveWorld(worldUid, out string? error)) {
            View.Print($"[WORLD REMOVE] Error: {error}");
            return;
        }

        bool removedActiveWorld =
            Simulation != null &&
            Simulation.World.Uid != null &&
            string.Equals(Simulation.World.Uid.Trim(), worldUid.Trim(), StringComparison.OrdinalIgnoreCase);

        if (removedActiveWorld) {
            Simulation = null;
            FrameBuffer = null;
        }

        View.Print($"[WORLD REMOVE] World '{worldUid}' removed successfully.");

        if (removedActiveWorld) {
            View.Print("[WORLD REMOVE] Removed world was active. Current in-memory simulation has been cleared.");
        }
    }
}
