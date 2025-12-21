using SimLabAPI;
using SimLabStatus;
using SimLabView;
using System.Reflection;

namespace SimLabController;

/// <summary>
/// This is the Command Interpreter - the main processing class of the Controller.
/// It executes all commands of the SimLab application and is responsible for communication with other parts.
/// </summary>
public class CmdInterpreter() {
    public StatusCode StatusCode { get; set; } = StatusCode.NoError;
    public bool QuitSignal { get; set; } = false;

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
        View.Print("Hello from main program method TestPlugIn.");

        // hard coded, for testing purposes
        string dllName = "SimLabPlugIn.dll";
        string className = "SimLabPlugIn.PlugIn";
        string methodName = "Update";

        MethodInfo? pluginMethod = null;

        if (!File.Exists(dllName)) {
            View.Print($"DLL file '{dllName}' not found.");
            return;
        }

        try {
            Assembly asm = Assembly.LoadFrom(dllName);
            Type? type = asm.GetType(className);
            if (type != null) {
                pluginMethod = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (pluginMethod == null) {
                    View.Print($"Public static method '{methodName}' not found in DLL '{dllName}'.");
                    return;
                }
            } else {
                View.Print($"Class '{className}' not found in DLL '{dllName}'.");
                return;
            }
        } catch (Exception ex) {
            View.Print($"Error loading DLL: {ex.Message}");
            return;
        }

        var api = new API();
        api.Test("main program");
        if (pluginMethod != null) {
            try {
                View.Print($"Calling plug-in method '{className}.{methodName}' from DLL '{dllName}'.");
                pluginMethod.Invoke(null, new object?[] { api });
                View.Print("Returned from plug-in method.");
            } catch (Exception ex) {
                View.Print($"Error calling plug-in method: {ex.Message}");
            }
        }
    }
}
