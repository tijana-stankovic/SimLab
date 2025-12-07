namespace SimLabController;

using SimLabStatus;
using SimLabView;
/// <summary>
/// The top-level class of the Controller.
/// It initializes the other application and Controller parts and executes the main application loop.
/// </summary>
public class Controller {
    private CmdInterpreter Interpreter { get; set; }

    public Controller(string[] args) {
        View.FullProgramInfo();

        Interpreter = new CmdInterpreter();
    }

    /// <summary>
    /// *** MAIN APPLICATION LOOP ***
    /// Runs the main application loop.
    /// Processes user commands until the "quit signal" (END command) is received.
    /// </summary>
    public void Run() {
        View.Print();

        bool quit = false;
        while (!quit) {
            View.PrintPrompt();
            Command cmd = CLI.ReadCommand();
            Interpreter.ExecuteCommand(cmd);
            quit = Interpreter.QuitSignal;
        }
    }
}
