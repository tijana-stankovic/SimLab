using SimLab.Status;

namespace SimLab.Output;

/// <summary>
/// A class with methods for displaying various information to the user and performing log data operations.
/// </summary>
internal class View {
    public static string FullLine { get; } = "--------------------------------------------------------------------------------";
    public static string FullDoubleLine { get; } = "================================================================================";
    public static bool DebugEnabled { get; set; } = false;

    static public void Print(string line = "") {
        Console.WriteLine(line);
    }

    static public void Print(string line, bool newLine) {
        if (newLine) {
            Console.WriteLine(line);
        } else {
            Console.Write(line);
        }
    }

    static public void Debug(string line = "") {
        if (!DebugEnabled) {
            return;
        }

        Print(line);
    }

    /// <summary>
    /// Displays information about the SimLab application.
    /// </summary>
    static public void FullProgramInfo() {
        string version = "1.0";
        string projectName = "SimLab - Simulation Laboratory";
        string course = "Bachelor thesis";
        string author = "Tijana Stankovic";
        string email = "tijana.stankovic@gmail.com";
        string supervisor = "RNDr. Michal Kopecký, Ph.D.";
        string university = "Charles University, Faculty of Mathematics and Physics";

        Print();
        Print(projectName + " [v " + version + "]");
        Print(course);
        Print("(c) " + author + ", " + email);
        Print("Supervisor: " + supervisor);
        Print(university);
        Print();
    }

    static public void PrintPrompt() {
        string prompt = "> ";
        Print(prompt, false);
    }

    static public void PrintStatus(StatusCode statusCode) {
        Print(StatusMessages.GetStatusMessage(statusCode));
    }
}
