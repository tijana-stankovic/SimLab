namespace SimLab.Configuration;

internal class DatabaseCfg {
    public string? Type { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string? Database { get; init; }
    public string? User { get; init; }
}
