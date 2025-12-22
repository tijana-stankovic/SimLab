using SimLab.Cmd;

namespace SimLab;

/// <summary>
/// Represents the SimLab application entry point.
/// This class provides the Main() method, which is the starting point of the application.
/// </summary>
public class SimLab {

    /// <summary>
    /// The starting point of the application.
    /// This method is called when the program starts. 
    /// It initializes the controller part of the application and passes control to the controller.
    /// Only one command line parameter is supported:
    /// 'configuration-file-name', which is the name of the SimLab configuration file.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the program.</param>
    public static void Main(string[] args) {
        if (args.Length <= 1) {
            Controller controller = new(args);
            controller.Run();
        } else {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage: simlab [configuration-file-name]");
        }
    }
}
