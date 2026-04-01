using Npgsql;
using SimLab.Configuration;
using SimLab.Simulator;
using SimLabApi;

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

    // Save complete world definition (world + characteristic + phase + phase_parameter) in one transaction.
    public bool AddWorldDefinition(WorldCfg worldCfg, out int worldId, out string? worldUid, out string? error) {
        worldId = 0;
        worldUid = null;

        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        if (worldCfg.Dimensions == null || worldCfg.Dimensions.Length < 2) {
            error = "World dimensions are missing or invalid.";
            return false;
        }

        if (worldCfg.Space != 2 && worldCfg.Space != 3) {
            error = $"Unsupported world space value '{worldCfg.Space}'. Expected 2 or 3.";
            return false;
        }

        if (worldCfg.Space == 3 && worldCfg.Dimensions.Length < 3) {
            error = "World space is 3, but dimensions do not contain Z value.";
            return false;
        }

        int dimX = worldCfg.Dimensions[0];
        int dimY = worldCfg.Dimensions[1];
        int dimZ = worldCfg.Space == 3 ? worldCfg.Dimensions[2] : 0;

        if (dimX < 0 || dimY < 0 || dimZ < 0) {
            error = "World dimensions cannot be negative.";
            return false;
        }

        char mode = ParseMode(worldCfg.Mode);
        string? requestedUid = string.IsNullOrWhiteSpace(worldCfg.Uid) ? null : worldCfg.Uid.Trim();

        var characteristicIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int[] characteristicIdByOrd = new int[worldCfg.Characteristics.Length];

        var seenCharacteristics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < worldCfg.Characteristics.Length; i++) {
            string characteristicName = worldCfg.Characteristics[i];
            if (!seenCharacteristics.Add(characteristicName)) {
                error = $"Duplicate characteristic name '{characteristicName}' in configuration.";
                return false;
            }
        }

        try {
            using var connection = DataSource!.OpenConnection();
            using var transaction = connection.BeginTransaction();

            try {
                if (requestedUid != null) {
                    using var existsCommand = new NpgsqlCommand(
                        "SELECT EXISTS (SELECT 1 FROM world WHERE upper(uid) = upper(@uid))",
                        connection,
                        transaction);
                    existsCommand.Parameters.AddWithValue("uid", requestedUid);

                    bool uidExists = (bool)existsCommand.ExecuteScalar()!;
                    if (uidExists) {
                        transaction.Rollback();
                        error = $"World with UID '{requestedUid}' already exists. Remove it first, then add again.";
                        return false;
                    }
                }

                using (var insertWorldCommand = new NpgsqlCommand(@"
                    INSERT INTO world (uid, name, space, x, y, z, mode)
                    VALUES (@uid, @name, @space, @x, @y, @z, @mode)
                    RETURNING id, uid", connection, transaction)) {
                    insertWorldCommand.Parameters.AddWithValue("uid", requestedUid ?? "");
                    insertWorldCommand.Parameters.AddWithValue("name", worldCfg.Name);
                    insertWorldCommand.Parameters.AddWithValue("space", worldCfg.Space);
                    insertWorldCommand.Parameters.AddWithValue("x", dimX);
                    insertWorldCommand.Parameters.AddWithValue("y", dimY);
                    insertWorldCommand.Parameters.AddWithValue("z", dimZ);
                    insertWorldCommand.Parameters.AddWithValue("mode", mode.ToString());

                    using var worldReader = insertWorldCommand.ExecuteReader();
                    if (!worldReader.Read()) {
                        throw new Exception("World insert failed: no row returned.");
                    }

                    worldId = worldReader.GetInt32(0);
                    worldUid = worldReader.GetString(1);
                }

                for (int i = 0; i < worldCfg.Characteristics.Length; i++) {
                    string characteristicName = worldCfg.Characteristics[i];

                    using var insertCharacteristicCommand = new NpgsqlCommand(@"
                        INSERT INTO characteristic (world, name, ord)
                        VALUES (@world, @name, @ord)
                        RETURNING id", connection, transaction);
                    insertCharacteristicCommand.Parameters.AddWithValue("world", worldId);
                    insertCharacteristicCommand.Parameters.AddWithValue("name", characteristicName);
                    insertCharacteristicCommand.Parameters.AddWithValue("ord", i);

                    int characteristicId = Convert.ToInt32(insertCharacteristicCommand.ExecuteScalar());
                    characteristicIdByName[characteristicName] = characteristicId;
                    characteristicIdByOrd[i] = characteristicId;
                }

                SavePhase(worldCfg.Initialization, PhaseName.Initialization, worldId, connection, transaction);
                SavePhase(worldCfg.PreCycle, PhaseName.PreCycle, worldId, connection, transaction);
                SavePhase(worldCfg.ProcessWorld, PhaseName.ProcessWorld, worldId, connection, transaction);
                SavePhase(worldCfg.Update, PhaseName.Update, worldId, connection, transaction);
                SavePhase(worldCfg.Evaluation, PhaseName.Evaluation, worldId, connection, transaction);
                SavePhase(worldCfg.Reproduction, PhaseName.Reproduction, worldId, connection, transaction);
                SavePhase(worldCfg.Selection, PhaseName.Selection, worldId, connection, transaction);
                SavePhase(worldCfg.PostCycle, PhaseName.PostCycle, worldId, connection, transaction);

                transaction.Commit();

                Cache.SetWorld(worldId);
                Cache.SetCharacteristicIdByName(characteristicIdByName);
                Cache.SetCharacteristicIdByOrd(characteristicIdByOrd);

                error = null;
                return true;
            } catch (Exception ex) {
                transaction.Rollback();
                worldId = 0;
                worldUid = null;
                error = ex.Message;
                return false;
            }
        } catch (Exception ex) {
            worldId = 0;
            worldUid = null;
            error = ex.Message;
            return false;
        }
    }

    // Parse mode text to a single character code. Default is 'S' (synchronous).
    private static char ParseMode(string? modeText) {
        if (string.IsNullOrWhiteSpace(modeText)) {
            return 'S';
        }

        switch (modeText.Trim().ToUpperInvariant()) {
            case "ASYNCHRONOUS":
                return 'A';
            case "SYNCHRONOUSCA":
            default:
                return 'S';
        }
    }

    // Save one phase (e.g. Initialization, PreCycle, etc.) and its parameters in the database.
    private static void SavePhase(
        MethodCfg? phaseConfig,
        PhaseName phaseName,
        int worldId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction) {

        if (phaseConfig == null) {
            return;
        }

        int phaseId;
        using (var insertPhaseCommand = new NpgsqlCommand(@"
            INSERT INTO phase (world, name, method)
            VALUES (@world, @name, @method)
            RETURNING id", connection, transaction)) {
            insertPhaseCommand.Parameters.AddWithValue("world", worldId);
            insertPhaseCommand.Parameters.AddWithValue("name", Phase.ToText(phaseName));
            insertPhaseCommand.Parameters.AddWithValue("method", phaseConfig.Method);

            phaseId = Convert.ToInt32(insertPhaseCommand.ExecuteScalar());
        }

        for (int i = 0; i < phaseConfig.Parameters.Length; i++) {
            using var insertParameterCommand = new NpgsqlCommand(@"
                INSERT INTO phase_parameter (phase, ord, value)
                VALUES (@phase, @ord, @value)", connection, transaction);
            insertParameterCommand.Parameters.AddWithValue("phase", phaseId);
            insertParameterCommand.Parameters.AddWithValue("ord", i);
            insertParameterCommand.Parameters.AddWithValue("value", phaseConfig.Parameters[i]);
            insertParameterCommand.ExecuteNonQuery();
        }
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

        // Without BeginTransaction(), the DELETE will be auto-committed immediately, 
        // which is fine for this single operation (no need for manual transaction.Commit()).
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
