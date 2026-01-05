using SimLab.ApiImplementation;
using SimLab.Configuration;
using SimLab.Simulator;
using SimLab.Status;
using SimLab.Output;
using SimLab.PlugInUtility;
using System.Reflection;

namespace SimLab.Cmd;

/// <summary>
/// This is the Command Interpreter - the main processing class of the Controller.
/// It executes all commands of the SimLab application and is responsible for communication with other parts.
/// </summary>
internal class CmdInterpreter {
    public StatusCode StatusCode { get; set; } = StatusCode.NoError;
    public bool QuitSignal { get; set; } = false;

    public Simulation Simulation { get; set; } = new Simulation();

    public CmdInterpreter(string[] args) {
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
        QuitSignal = true;
    }

    /// <summary>
    /// TEST command entry point.
    /// Tests the plug-in functionality.
    /// </summary>
    static private void TestPlugIn() {
        // hard coded, for testing purposes
        string plugInMethodPath = "SimLabPlugIn.dll;SimLabPlugIn.PlugIn;Update";

        if (PlugIn.ParseMethodPath(plugInMethodPath,
            out string dllName, 
            out string className, 
            out string methodName, 
            out string? error)) {

            var pluginMethod = PlugIn.GetMethod(dllName, className, methodName, out error);

            if (pluginMethod != null) {
                View.Print("Hello from the main program method TestPlugIn.");
                var api = new API();
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

    private void LoadConfigurationFile(string fileName) {
        Console.WriteLine("[Info] Loading configuration from JSON file: " + fileName);
        if (Json.LoadConfiguration(fileName, out WorldCfg? WorldCfg)) {
            Simulation.WorldConfiguration = WorldCfg;
            if (WorldCfg != null) {
                Console.WriteLine("[Info] Configuration successfully loaded from JSON file.");
                if (WorldCfg.Initialization != null)
                    Simulation.InitializationMethod = GetMethod(WorldCfg.Initialization.Method);
                if (WorldCfg.Update != null)
                    Simulation.UpdateMethod = GetMethod(WorldCfg.Update.Method);
                if (WorldCfg.Evaluation != null)
                    Simulation.EvaluationMethod = GetMethod(WorldCfg.Evaluation.Method);
                if (WorldCfg.Reproduction != null)
                    Simulation.ReproductionMethod = GetMethod(WorldCfg.Reproduction.Method);
                if (WorldCfg.Selection != null)
                    Simulation.SelectionMethod = GetMethod(WorldCfg.Selection.Method);
            }
        } else {
            Console.WriteLine("[Warning] No valid configuration loaded from JSON file. Running without simulation world.");
        }
    }
}
