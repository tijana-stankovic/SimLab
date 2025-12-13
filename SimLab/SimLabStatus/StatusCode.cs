namespace SimLabStatus;

/// <summary>
/// Enum representing various status codes for the SimLab application.
/// </summary>
public enum StatusCode {
    /// <summary>
    /// No error.
    /// </summary>
    NoError,

    /// <summary>
    /// Unexpected program status.
    /// </summary>
    UnexpectedStatus,

    /// <summary>
    /// Unknown command.
    /// </summary>
    UnknownCommand,

    /// <summary>
    /// The SimLab command has an invalid number of arguments.
    /// </summary>
    InvalidNumberOfArguments,

    /// <summary>
    /// The SimLab command has an invalid argument value.
    /// </summary>
    InvalidArgument,
}
