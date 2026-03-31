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
            LoadConfigurationFile(args[0]);
        }
    }

    /// <summary>
    /// Entry point for command processing. Executes the specified command.
    /// </summary>
    /// <param name="cmd">The command to execute.</param>
    public void ExecuteCommand(Command cmd) {
        StatusCode = StatusCode.NoError;

        string command = cmd.Name.ToUpper();

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

            case "TS":
            case "TESTSIM":
                if (cmd.Args.Length != 1) {
                    View.Print("Invalid number of arguments for TESTSIM command.");
                    View.Print("To specify the number of cycles, use: TESTSIM <number-of-cycles>");
                    break;
                } else if (int.TryParse(cmd.Args[0], out int numberOfCycles)) {
                    TestSim(numberOfCycles);
                }
                break;

            case "W":
            case "WORLD":
                World(cmd.Args);
                break;

            case "S":
            case "SHOW":
                if (cmd.Args.Length != 0) {
                    View.Print("SHOW command does not accept arguments.");
                    break;
                }
                Show();
                break;

            case "T":
            case "TEST":
                TestPlugIn();
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
        View.Print("- TESTSIM (TS) <number-of-cycles>");
        View.Print("  Run the simulation for the specified number of cycles.");
        View.Print("- SHOW (S)");
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

    /// <summary>
    /// TEST command entry point.
    /// Tests the plug-in functionality.
    /// </summary>
    private void TestPlugIn() {
        // hard coded, for testing purposes
        string plugInMethodPath = "SimLabPlugIn.dll;SimLabPlugIn.PlugIn;Test";

        if (PlugIn.ParseMethodPath(plugInMethodPath,
            out string dllName, 
            out string className, 
            out string methodName, 
            out string? error)) {

            var pluginMethod = PlugIn.GetMethod(dllName, className, methodName, out error);

            if (pluginMethod != null) {
                View.Print("Hello from the main program method TestPlugIn.");
                var api = new API(Simulation);
                api.Test("main program");
                View.Print($"Calling plug-in method '{className}.{methodName}' from DLL '{dllName}'.");
                if (PlugIn.Execute(pluginMethod, api, out error)) {
                    View.Print("Returned from plug-in method.");
                } else {
                    View.Print($"Error calling plug-in method.");
                    View.Print($"Error text:\n{error}");
                }
            } else {
                View.Print($"Error retrieving plug-in method: '{methodName}' from class '{className}' in DLL '{dllName}'.");
                View.Print($"Error text:\n{error}");
            }
        } else {
            View.Print($"Error parsing plug-in method path: '{plugInMethodPath}'");
            View.Print($"Error text:\n{error}");
        }
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

    private void LoadConfigurationFile(string fileName) {
        View.Print("[Info] Loading configuration from JSON file: " + fileName);
        if (ConfigJson.LoadConfiguration(fileName, out WorldCfg? WorldCfg)) {
            if (WorldCfg != null) {
                Characteristics.Init(WorldCfg.Characteristics);
                Simulation = new Simulation(new World(WorldCfg));
                FrameBuffer = new FrameBuffer(Simulation.World);
                Simulation.Mode = ParseModeOrDefault(WorldCfg.Mode, out bool invalidMode);
                if (invalidMode) {
                    View.Print($"[Warning] Unknown simulation mode '{WorldCfg.Mode}'. Using '{Simulation.Mode}'.");
                }
                View.Print("[Info] Configuration successfully loaded from JSON file.");
                View.Print($"[Info] Simulation mode: {Simulation.Mode}");
                if (WorldCfg.Initialization != null) {
                    Simulation.InitializationMethod = GetMethod(WorldCfg.Initialization.Method);
                    Simulation.InitializationParameters = WorldCfg.Initialization.Parameters;
                }
                if (WorldCfg.PreCycle != null) {
                    Simulation.PreCycleMethod = GetMethod(WorldCfg.PreCycle.Method);
                    Simulation.PreCycleParameters = WorldCfg.PreCycle.Parameters;
                }
                if (WorldCfg.ProcessWorld != null) {
                    Simulation.ProcessWorldMethod = GetMethod(WorldCfg.ProcessWorld.Method);
                    Simulation.ProcessWorldParameters = WorldCfg.ProcessWorld.Parameters;
                }
                if (WorldCfg.Update != null) {
                    Simulation.UpdateMethod = GetMethod(WorldCfg.Update.Method);
                    Simulation.UpdateParameters = WorldCfg.Update.Parameters;
                }
                if (WorldCfg.Evaluation != null) {
                    Simulation.EvaluationMethod = GetMethod(WorldCfg.Evaluation.Method);
                    Simulation.EvaluationParameters = WorldCfg.Evaluation.Parameters;
                }
                if (WorldCfg.Reproduction != null) {
                    Simulation.ReproductionMethod = GetMethod(WorldCfg.Reproduction.Method);
                    Simulation.ReproductionParameters = WorldCfg.Reproduction.Parameters;
                }
                if (WorldCfg.Selection != null) {
                    Simulation.SelectionMethod = GetMethod(WorldCfg.Selection.Method);
                    Simulation.SelectionParameters = WorldCfg.Selection.Parameters;
                }
                if (WorldCfg.PostCycle != null) {
                    Simulation.PostCycleMethod = GetMethod(WorldCfg.PostCycle.Method);
                    Simulation.PostCycleParameters = WorldCfg.PostCycle.Parameters;
                }
            }
        } else {
            View.Print("[Warning] No valid configuration loaded from JSON file. Running without simulation world.");
        }
    }

    private void Show() {
        if (FrameBuffer == null || !FrameBuffer.HasFrames) {
            View.Print("[Show] No generated frames available. Run TESTSIM first.");
            return;
        }

        try {
            int frameIndex = Visualizer.Show(FrameBuffer);
            if (frameIndex == 0) {
                View.Print($"[Show] Closed visualization on the initial cells position.");
            } else if (frameIndex > 0) {
                View.Print($"[Show] Closed visualization on frame {frameIndex}/{FrameBuffer.Count - 1}.");
            }
        } catch (Exception ex) {
            View.Print($"[Show] Visualization error: {ex.Message}");
        }
    }

    private void TestSim(int numberOfCycles) {
        if (Simulation == null) {
            View.Print("No simulation created from configuration file. Cannot run TestSim.");
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
                View.Print("[TestSim] Calling plug-in initialization method.");
                if (PlugIn.Execute(sim.InitializationMethod, api, out var error))
                    View.Print("[TestSim] Initialization completed successfully.");
                else
                    View.Print($"[TestSim] Initialization error: {error}");
            }
            sim.EndCycle();
            FrameBuffer?.Capture(sim);
            PrintCellCharacteristics(sim, "Initial characteristics of all cells:");
        } else {
            PrintCellCharacteristics(sim, "Current characteristics of all cells:");
        }


        // Main simulation loop
        for (int i = 0; i < numberOfCycles; i++) {
            sim.Cycle++;
            View.Print($"\n[TestSim] Starting cycle {sim.Cycle}.");
            sim.BeginCycle();

            void ExecuteIfNotNull(MethodInfo? method) {
                if (method != null) {
                    if (PlugIn.Execute(method, api, out var error)) 
                        View.Print($"    [TestSim] Plug-in method {method.Name} executed successfully.");
                    else 
                        View.Print($"    [TestSim] Error executing plug-in method {method.Name}: {error}");
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

            PrintCellCharacteristics(sim, "Characteristics of all cells after cycle " + sim.Cycle + ":");
        }

        View.Print(""); View.Print(""); View.Print("");
    }

    // go through all cells and print their characteristics
    static private void PrintCellCharacteristics(Simulation sim, string header) {
        View.Print($"\n[TestSim] {header}");
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
        View.Print("id      uid             name                     space    x      y      z      mode");

        foreach (DbWorldInfo worldInfo in worlds) {
            View.Print(
                $"{worldInfo.Id,-8}{worldInfo.Uid,-16}{worldInfo.Name,-25}{worldInfo.Space,-9}{worldInfo.X,-7}{worldInfo.Y,-7}{worldInfo.Z,-7}{worldInfo.Mode}");
        }
    }

    private void WorldAdd(string jsonConfigFile) {
        View.Print($"[WORLD ADD] Not implemented yet. Input file: {jsonConfigFile}");
    }

    private void WorldLoad(string worldUid) {
        View.Print($"[WORLD LOAD] Not implemented yet. World UID: {worldUid}");
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

        View.Print($"[WORLD REMOVE] World '{worldUid}' removed successfully.");
    }
}
