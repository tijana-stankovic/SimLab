namespace SimLab.Cmd;

/// <summary>
/// A class providing internal supporting structure for a user command.
/// </summary>
internal class Command(string cmdName, string[] cmdArgs) {
    public string Name { get; } = cmdName;
    public string[] Args { get; } = cmdArgs;
}
