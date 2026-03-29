using SimLab.Simulator;

namespace SimLab.DB;

internal interface IDatabase {
    bool IsConnected { get; }
    bool Connect(out string? error);
    void Disconnect();
    bool TestConnection(out string? error);
    bool TryGetConnectionInfo(out string? userName, out string? databaseName, out string? error);
    bool SaveCurrentState(Simulation simulation, out string? error);
    bool TryLoadState(long stateId, Simulation simulation, out string? error);
}
