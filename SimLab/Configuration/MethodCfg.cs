namespace SimLab.Configuration;

internal class MethodCfg {
    public required string Method { get; init; }
    public string[] Parameters { get; init; } = Array.Empty<string>();
}