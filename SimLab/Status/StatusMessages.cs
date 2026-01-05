namespace SimLab.Status;

/// <summary>
/// Class for mapping status codes to their respective messages.
/// </summary>
internal class StatusMessages {
    // Initializes the status messages for each status code.
    private static readonly Dictionary<StatusCode, string> statusMessages = new() {
        { StatusCode.NoError, "No error." },
        { StatusCode.UnexpectedStatus, "WARNING: Unexpected program status." },
        { StatusCode.UnknownCommand, "ERROR: Unknown command. Use Help or H for a list of available commands." },
        { StatusCode.InvalidNumberOfArguments, "ERROR: Invalid number of arguments." },
        { StatusCode.InvalidArgument, "ERROR: Invalid argument value." },
    };

    public static string GetStatusMessage(StatusCode statusCode) {
        return statusMessages.TryGetValue(statusCode, out string? message)
            ? message
            : "WARNING: Unknown program status code.";
    }
}
