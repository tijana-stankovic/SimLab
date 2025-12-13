namespace SimLabController;

/// <summary>
/// A class providing internal supporting structure for a user command.
/// </summary>
public class Command(string cmdName, string[] cmdArgs) {
    public string Name { get; } = cmdName;
    public string[] Args { get; } = cmdArgs;
}
