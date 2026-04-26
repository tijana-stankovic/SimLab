using SimLab.Configuration;
using SimLab.Simulator;

namespace SimLab.DB;

internal interface IDatabase {
    bool IsConnected { get; }
    bool Connect(out string? error);
    void Disconnect();
    bool TestConnection(out string? error);
    bool GetConnectionInfo(out string? userName, out string? databaseName, out string? error);
    bool ListWorlds(out List<DbWorldInfo> worlds, out string? error);
    bool AddWorldDefinition(WorldCfg worldCfg, out int worldId, out string? worldUid, out string? error);
    bool LoadWorldDefinition(
        string worldUid,
        out int worldId,
        out WorldCfg? worldCfg,
        out long? lastCycle,
        out long nextCellId,
        out long? lastViewedFrame,
        out string? error);
    bool RemoveWorld(string worldUid, out string? error);
    bool ResetWorldSimulation(int worldId, out string? error);
    bool UpdateWorldLastViewedFrame(int worldId, long lastViewedFrame, out string? error);
    bool SaveCurrentState(Simulation simulation, out string? error);
    bool LoadState(int worldId, long cycleNumber, Simulation simulation, out string? error);
}
