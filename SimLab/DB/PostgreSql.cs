using Npgsql;
using SimLab.Configuration;
using SimLab.Simulator;

namespace SimLab.DB;

internal class PostgreSql(string connectionString) : IDatabase {
    private string ConnectionString { get; } = connectionString;
    private NpgsqlDataSource? DataSource { get; set; }
    private DbCache Cache { get; } = new();

    public bool IsConnected => DataSource != null;

    public bool Connect(out string? error) {
        if (IsConnected) {
            return TestConnection(out error);
        }

        if (string.IsNullOrWhiteSpace(ConnectionString)) {
            error = "Connection string is empty.";
            return false;
        }

        try {
            DataSource = NpgsqlDataSource.Create(ConnectionString);
            return TestConnection(out error);
        } catch (Exception ex) {
            DataSource = null;
            error = ex.Message;
            return false;
        }
    }

    public void Disconnect() {
        if (DataSource != null) {
            DataSource.Dispose();
            DataSource = null;
        }

        Cache.Clear();
    }

    public bool TestConnection(out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        try {
            using var connection = DataSource!.OpenConnection();
            error = null;
            return true;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    public bool GetConnectionInfo(out string? userName, out string? databaseName, out string? error) {
        userName = null;
        databaseName = null;

        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        try {
            using var connection = DataSource!.OpenConnection();
            using var command = new NpgsqlCommand("SELECT current_user, current_database()", connection);
            using var reader = command.ExecuteReader();

            if (reader.Read()) {
                userName = reader.GetString(0);
                databaseName = reader.GetString(1);
                error = null;
                return true;
            }

            error = "Unable to read connection info.";
            return false;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    // List all worlds in the database, ordered by their ID.
    public bool ListWorlds(out List<DbWorldInfo> worlds, out string? error) {
        worlds = [];

        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        try {
            using var connection = DataSource!.OpenConnection();
            using var command = new NpgsqlCommand(@"
                SELECT id, uid, name, space, x, y, z, mode
                FROM world
                ORDER BY id", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read()) {
                string modeText = reader.GetString(7);

                worlds.Add(new DbWorldInfo {
                    Id = reader.GetInt32(0),
                    Uid = reader.GetString(1),
                    Name = reader.GetString(2),
                    Space = reader.GetInt32(3),
                    X = reader.GetInt32(4),
                    Y = reader.GetInt32(5),
                    Z = reader.GetInt32(6),
                    Mode = modeText.Length > 0 ? modeText[0] : '?'
                });
            }

            error = null;
            return true;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    // TODO: Implement world definition insert transaction
    public bool AddWorldDefinition(WorldCfg worldCfg, out int worldId, out string? worldUid, out string? error) {
        worldId = 0;
        worldUid = null;

        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        error = "AddWorldDefinition is not implemented yet.";
        return false;
    }

    // TODO: Implement world definition load transaction
    public bool LoadWorldDefinition(string worldUid, out int worldId, out WorldCfg? worldCfg, out string? error) {
        worldId = 0;
        worldCfg = null;

        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        error = "LoadWorldDefinition is not implemented yet.";
        return false;
    }

    // Remove world specified by world UID. 
    // If the removed world is currently cached, clear the cache as well.
    public bool RemoveWorld(string worldUid, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(worldUid)) {
            error = "World UID is empty.";
            return false;
        }

        try {
            using var connection = DataSource!.OpenConnection();
            using var command = new NpgsqlCommand(
                "DELETE FROM world WHERE upper(uid) = upper(@uid) RETURNING id", connection);

            command.Parameters.AddWithValue("uid", worldUid.Trim());
            object? removedWorldIdObj = command.ExecuteScalar();

            if (removedWorldIdObj == null) {
                error = $"World with UID '{worldUid}' was not found.";
                return false;
            }

            int removedWorldId = Convert.ToInt32(removedWorldIdObj);

            if (Cache.WorldId.HasValue && Cache.WorldId.Value == removedWorldId) {
                Cache.Clear();
            }

            error = null;
            return true;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    // TODO: Implement SaveCurrentState method to save simulation state to the database.
    public bool SaveCurrentState(Simulation simulation, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        error = "SaveCurrentState is not implemented yet.";
        return false;
    }

    // TODO: Implement LoadState method to load simulation state from the database.
    public bool LoadState(long stateId, Simulation simulation, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        error = "LoadState is not implemented yet.";
        return false;
    }
}
