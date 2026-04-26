using Npgsql;
using SimLab.Configuration;
using SimLab.Simulator;
using SimColor = SimLab.Simulator.Color;
using SimulatorPosition = SimLab.Simulator.Position;
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
                SELECT id, uid, name, space, x, y, z, mode, last_cycle, next_cell_id, last_viewed_frame
                FROM world
                ORDER BY id", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read()) {
                string modeText = reader.GetString(7);
                long? lastCycle = reader.IsDBNull(8) ? null : reader.GetInt64(8);
                long nextCellId = reader.GetInt64(9);
                long? lastViewedFrame = reader.IsDBNull(10) ? null : reader.GetInt64(10);

                worlds.Add(new DbWorldInfo {
                    Id = reader.GetInt32(0),
                    Uid = reader.GetString(1),
                    Name = reader.GetString(2),
                    Space = reader.GetInt32(3),
                    X = reader.GetInt32(4),
                    Y = reader.GetInt32(5),
                    Z = reader.GetInt32(6),
                    Mode = modeText.Length > 0 ? modeText[0] : '?',
                    LastCycle = lastCycle,
                    NextCellId = nextCellId,
                    LastViewedFrame = lastViewedFrame
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

        if (worldCfg.Dimensions == null || worldCfg.Dimensions.Length is not (1 or 2 or 3)) {
            error = "World dimensions are missing or invalid.";
            return false;
        }

        if (worldCfg.Space is not (1 or 2 or 3)) {
            error = $"Unsupported world space value '{worldCfg.Space}'. Expected 1, 2 or 3.";
            return false;
        }

        if (worldCfg.Dimensions.Length < worldCfg.Space) {
            error = $"World space is {worldCfg.Space}, but dimensions contain only {worldCfg.Dimensions.Length} values.";
            return false;
        }

        int dimX = worldCfg.Dimensions[0];
        int dimY = worldCfg.Space >= 2 ? worldCfg.Dimensions[1] : 0;
        int dimZ = worldCfg.Space == 3 ? worldCfg.Dimensions[2] : 0;

        if (dimX < 0 || dimY < 0 || dimZ < 0) {
            error = "World dimensions cannot be negative.";
            return false;
        }

        char mode = ParseMode(worldCfg.Mode);
        int foregroundPacked = PackRgb(worldCfg.Foreground, World.DefaultForegroundColor);
        int backgroundPacked = PackRgb(worldCfg.Background, World.DefaultBackgroundColor);
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
                    INSERT INTO world (uid, name, space, x, y, z, mode, foreground, background, last_cycle, next_cell_id, last_viewed_frame)
                    VALUES (@uid, @name, @space, @x, @y, @z, @mode, @foreground, @background, @last_cycle, @next_cell_id, @last_viewed_frame)
                    RETURNING id, uid", connection, transaction)) {
                    insertWorldCommand.Parameters.AddWithValue("uid", requestedUid ?? "");
                    insertWorldCommand.Parameters.AddWithValue("name", worldCfg.Name);
                    insertWorldCommand.Parameters.AddWithValue("space", worldCfg.Space);
                    insertWorldCommand.Parameters.AddWithValue("x", dimX);
                    insertWorldCommand.Parameters.AddWithValue("y", dimY);
                    insertWorldCommand.Parameters.AddWithValue("z", dimZ);
                    insertWorldCommand.Parameters.AddWithValue("mode", mode.ToString());
                    insertWorldCommand.Parameters.AddWithValue("foreground", foregroundPacked);
                    insertWorldCommand.Parameters.AddWithValue("background", backgroundPacked);
                    insertWorldCommand.Parameters.AddWithValue("last_cycle", NpgsqlTypes.NpgsqlDbType.Bigint, DBNull.Value);
                    insertWorldCommand.Parameters.AddWithValue("next_cell_id", 1L);
                    insertWorldCommand.Parameters.AddWithValue("last_viewed_frame", NpgsqlTypes.NpgsqlDbType.Bigint, DBNull.Value);

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

    private static int PackRgb(int[]? rgb, SimColor defaultColor) {
        byte r;
        byte g;
        byte b;

        if (rgb == null || rgb.Length < 3) {
            r = defaultColor.R;
            g = defaultColor.G;
            b = defaultColor.B;
        } else {
            r = (byte)rgb[0];
            g = (byte)rgb[1];
            b = (byte)rgb[2];
        }

        return (r << 16) | (g << 8) | b;
    }

    private static int[] UnpackRgb(int packedRgb) {
        int r = (packedRgb >> 16) & 255;
        int g = (packedRgb >> 8) & 255;
        int b = packedRgb & 255;
        return new int[] { r, g, b };
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

    // Load full world definition from database by world UID.
    public bool LoadWorldDefinition(
        string worldUid,
        out int worldId,
        out WorldCfg? worldCfg,
        out long? lastCycle,
        out long nextCellId,
        out long? lastViewedFrame,
        out string? error) {
        worldId = 0;
        worldCfg = null;
        lastCycle = null;
        nextCellId = 1;
        lastViewedFrame = null;

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

            string? loadedUid = null;
            string? loadedName = null;
            int loadedSpace = 0;
            int dimX = 0;
            int dimY = 0;
            int dimZ = 0;
            string? loadedMode = null;
            int loadedForeground = PackRgb(null, World.DefaultForegroundColor);
            int loadedBackground = PackRgb(null, World.DefaultBackgroundColor);

            using (var worldCommand = new NpgsqlCommand(@"
                SELECT id, uid, name, space, x, y, z, mode, foreground, background, last_cycle, next_cell_id, last_viewed_frame
                FROM world
                WHERE upper(uid) = upper(@uid)", connection)) {
                worldCommand.Parameters.AddWithValue("uid", worldUid.Trim());

                using var worldReader = worldCommand.ExecuteReader();
                if (!worldReader.Read()) {
                    error = $"World with UID '{worldUid}' was not found.";
                    return false;
                }

                worldId = worldReader.GetInt32(0);
                loadedUid = worldReader.GetString(1);
                loadedName = worldReader.GetString(2);
                loadedSpace = worldReader.GetInt16(3);
                dimX = worldReader.GetInt32(4);
                dimY = worldReader.GetInt32(5);
                dimZ = worldReader.GetInt32(6);
                string modeCode = worldReader.GetString(7);
                loadedMode = ModeCodeToText(modeCode);
                loadedForeground = worldReader.GetInt32(8);
                loadedBackground = worldReader.GetInt32(9);
                lastCycle = worldReader.IsDBNull(10) ? null : worldReader.GetInt64(10);
                nextCellId = worldReader.GetInt64(11);
                lastViewedFrame = worldReader.IsDBNull(12) ? null : worldReader.GetInt64(12);
            }

            if (loadedUid == null || loadedName == null || loadedMode == null) {
                error = "World data is incomplete in database.";
                return false;
            }

            var characteristicNames = new List<string>();
            var characteristicIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var characteristicIdByOrd = new List<int>();

            using (var characteristicCommand = new NpgsqlCommand(@"
                SELECT id, name, ord
                FROM characteristic
                WHERE world = @world
                ORDER BY ord", connection)) {
                characteristicCommand.Parameters.AddWithValue("world", worldId);

                using var characteristicReader = characteristicCommand.ExecuteReader();

                int expectedOrd = 0;
                while (characteristicReader.Read()) {
                    int characteristicId = characteristicReader.GetInt32(0);
                    string characteristicName = characteristicReader.GetString(1);
                    int characteristicOrd = characteristicReader.GetInt32(2);

                    if (characteristicOrd != expectedOrd) {
                        error = $"Invalid characteristic order for world '{loadedUid}'. Expected ord={expectedOrd}, found ord={characteristicOrd}.";
                        return false;
                    }

                    characteristicNames.Add(characteristicName);
                    characteristicIdByName[characteristicName] = characteristicId;
                    characteristicIdByOrd.Add(characteristicId);
                    expectedOrd++;
                }
            }

            MethodCfg? initialization = null;
            MethodCfg? preCycle = null;
            MethodCfg? processWorld = null;
            MethodCfg? update = null;
            MethodCfg? evaluation = null;
            MethodCfg? reproduction = null;
            MethodCfg? selection = null;
            MethodCfg? postCycle = null;

            var phases = new List<(int PhaseId, string Name, string Method)>();

            using (var phaseCommand = new NpgsqlCommand(@"
                SELECT id, name, method
                FROM phase
                WHERE world = @world
                ORDER BY id", connection)) {
                phaseCommand.Parameters.AddWithValue("world", worldId);

                using var phaseReader = phaseCommand.ExecuteReader();
                while (phaseReader.Read()) {
                    phases.Add((
                        phaseReader.GetInt32(0),
                        phaseReader.GetString(1),
                        phaseReader.GetString(2)));
                }
            }

            foreach (var phaseRow in phases) {
                if (!Phase.TryToValue(phaseRow.Name, out PhaseName phaseName)) {
                    error = $"Unsupported phase name '{phaseRow.Name}' found in database.";
                    return false;
                }

                string[] parameters = ReadPhaseParameters(connection, phaseRow.PhaseId, out string? parametersError);
                if (parametersError != null) {
                    error = parametersError;
                    return false;
                }

                var methodCfg = new MethodCfg {
                    Method = phaseRow.Method,
                    Parameters = parameters
                };

                switch (phaseName) {
                    case PhaseName.Initialization:
                        if (initialization != null) { error = "Duplicate Initialization phase found in database."; return false; }
                        initialization = methodCfg;
                        break;
                    case PhaseName.PreCycle:
                        if (preCycle != null) { error = "Duplicate PreCycle phase found in database."; return false; }
                        preCycle = methodCfg;
                        break;
                    case PhaseName.ProcessWorld:
                        if (processWorld != null) { error = "Duplicate ProcessWorld phase found in database."; return false; }
                        processWorld = methodCfg;
                        break;
                    case PhaseName.Update:
                        if (update != null) { error = "Duplicate Update phase found in database."; return false; }
                        update = methodCfg;
                        break;
                    case PhaseName.Evaluation:
                        if (evaluation != null) { error = "Duplicate Evaluation phase found in database."; return false; }
                        evaluation = methodCfg;
                        break;
                    case PhaseName.Reproduction:
                        if (reproduction != null) { error = "Duplicate Reproduction phase found in database."; return false; }
                        reproduction = methodCfg;
                        break;
                    case PhaseName.Selection:
                        if (selection != null) { error = "Duplicate Selection phase found in database."; return false; }
                        selection = methodCfg;
                        break;
                    case PhaseName.PostCycle:
                        if (postCycle != null) { error = "Duplicate PostCycle phase found in database."; return false; }
                        postCycle = methodCfg;
                        break;
                }
            }

            int[] dimensions = loadedSpace == 3 ? new int[] { dimX, dimY, dimZ } : new int[] { dimX, dimY };

            worldCfg = new WorldCfg {
                Uid = loadedUid,
                Name = loadedName,
                Space = loadedSpace,
                Dimensions = dimensions,
                Characteristics = characteristicNames.ToArray(),
                Foreground = UnpackRgb(loadedForeground),
                Background = UnpackRgb(loadedBackground),
                Mode = loadedMode,
                Initialization = initialization,
                PreCycle = preCycle,
                ProcessWorld = processWorld,
                Update = update,
                Evaluation = evaluation,
                Reproduction = reproduction,
                Selection = selection,
                PostCycle = postCycle
            };

            Cache.SetWorld(worldId);
            Cache.SetCharacteristicIdByName(characteristicIdByName);
            Cache.SetCharacteristicIdByOrd(characteristicIdByOrd.ToArray());

            error = null;
            return true;
        } catch (Exception ex) {
            worldId = 0;
            worldCfg = null;
            lastCycle = null;
            nextCellId = 1;
            lastViewedFrame = null;
            error = ex.Message;
            return false;
        }
    }
    
    // Convert mode code from database to text
    private static string? ModeCodeToText(string? modeCode) {
        if (string.IsNullOrWhiteSpace(modeCode)) {
            return null;
        }

        switch (modeCode.Trim().ToUpperInvariant()) {
            case "S":
                return "SynchronousCA";
            case "A":
                return "Asynchronous";
            default:
                return null;
        }
    }
    
    // Load all parameters for the given phase ID
    private static string[] ReadPhaseParameters(NpgsqlConnection connection, int phaseId, out string? error) {
        error = null;
        var parameters = new List<string>();

        using var parameterCommand = new NpgsqlCommand(@"
            SELECT ord, value
            FROM phase_parameter
            WHERE phase = @phase
            ORDER BY ord", connection);
        parameterCommand.Parameters.AddWithValue("phase", phaseId);

        using var parameterReader = parameterCommand.ExecuteReader();

        int expectedOrd = 0;
        while (parameterReader.Read()) {
            int parameterOrd = parameterReader.GetInt32(0);
            string parameterValue = parameterReader.GetString(1);

            if (parameterOrd != expectedOrd) {
                error = $"Invalid phase_parameter order for phase id={phaseId}. Expected ord={expectedOrd}, found ord={parameterOrd}.";
                return [];
            }

            parameters.Add(parameterValue);
            expectedOrd++;
        }

        return parameters.ToArray();
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

    public bool ResetWorldSimulation(int worldId, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        if (worldId <= 0) {
            error = $"Invalid world ID '{worldId}'.";
            return false;
        }

        try {
            using var connection = DataSource!.OpenConnection();
            using var transaction = connection.BeginTransaction();

            try {
                // Delete all cycles (and CASCADE DELETE simunits and simunit_data) for the world.
                using (var deleteCyclesCommand = new NpgsqlCommand(@"
                    DELETE FROM cycle
                    WHERE world = @world_id", connection, transaction)) {
                    deleteCyclesCommand.Parameters.AddWithValue("world_id", worldId);
                    deleteCyclesCommand.ExecuteNonQuery();
                }

                using (var updateWorldCommand = new NpgsqlCommand(@"
                    UPDATE world
                    SET last_cycle = @last_cycle,
                        next_cell_id = @next_cell_id,
                        last_viewed_frame = @last_viewed_frame
                    WHERE id = @world_id", connection, transaction)) {
                    updateWorldCommand.Parameters.AddWithValue("last_cycle", NpgsqlTypes.NpgsqlDbType.Bigint, DBNull.Value);
                    updateWorldCommand.Parameters.AddWithValue("next_cell_id", 1L);
                    updateWorldCommand.Parameters.AddWithValue("last_viewed_frame", NpgsqlTypes.NpgsqlDbType.Bigint, DBNull.Value);
                    updateWorldCommand.Parameters.AddWithValue("world_id", worldId);

                    int affectedRows = updateWorldCommand.ExecuteNonQuery();
                    if (affectedRows != 1) {
                        throw new Exception($"World update failed for id={worldId}.");
                    }
                }

                transaction.Commit();
                error = null;
                return true;
            } catch (Exception ex) {
                transaction.Rollback();
                error = ex.Message;
                return false;
            }
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    public bool UpdateWorldLastViewedFrame(int worldId, long lastViewedFrame, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        if (worldId <= 0) {
            error = $"Invalid world ID '{worldId}'.";
            return false;
        }

        if (lastViewedFrame < 0) {
            error = $"Invalid last viewed frame '{lastViewedFrame}'.";
            return false;
        }

        try {
            using var connection = DataSource!.OpenConnection();
            using var command = new NpgsqlCommand(@"
                UPDATE world
                SET last_viewed_frame = @last_viewed_frame
                WHERE id = @world_id", connection);

            command.Parameters.AddWithValue("last_viewed_frame", lastViewedFrame);
            command.Parameters.AddWithValue("world_id", worldId);

            int affectedRows = command.ExecuteNonQuery();
            if (affectedRows != 1) {
                error = $"World with ID '{worldId}' was not found.";
                return false;
            }

            error = null;
            return true;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    // Save current simulation state to the database.
    public bool SaveCurrentState(Simulation simulation, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        int? worldIdOrNull = simulation.World.Id ?? Cache.WorldId;
        if (!worldIdOrNull.HasValue) {
            error = "Active world ID is not available. Load or add a world before saving state.";
            return false;
        }

        int worldId = worldIdOrNull.Value;
        long cycleNumber = simulation.Cycle;
        long nextCellId = simulation.GetNextCellId();

        if (cycleNumber < 0) {
            error = $"Invalid cycle value '{cycleNumber}'.";
            return false;
        }

        long simunitCount = simulation.GetCellCount();
        int characteristicCount = Characteristics.Count;
        int[] characteristicIdByOrd = new int[characteristicCount];

        for (int i = 0; i < characteristicCount; i++) {
            if (!Cache.TryGetCharacteristicId(i, out int characteristicId)) {
                error = $"Missing characteristic DB mapping for ord={i}. Load or add world definition before saving state.";
                return false;
            }

            characteristicIdByOrd[i] = characteristicId;
        }

        try {
            using var connection = DataSource!.OpenConnection();
            using var transaction = connection.BeginTransaction();

            try {
                long cycleId;

                using (var insertCycleCommand = new NpgsqlCommand(@"
                    INSERT INTO cycle (world, cycle, simunit_count)
                    VALUES (@world, @cycle, @simunit_count)
                    RETURNING id", connection, transaction)) {
                    insertCycleCommand.Parameters.AddWithValue("world", worldId);
                    insertCycleCommand.Parameters.AddWithValue("cycle", cycleNumber);
                    insertCycleCommand.Parameters.AddWithValue("simunit_count", simunitCount);

                    object? cycleIdObj = insertCycleCommand.ExecuteScalar();
                    if (cycleIdObj == null) {
                        throw new Exception("Cycle insert failed: no row returned.");
                    }

                    cycleId = Convert.ToInt64(cycleIdObj);
                }

                using (var insertSimunitCommand = new NpgsqlCommand(@"
                    INSERT INTO simunit (cycle, simunit_id, x, y, z)
                    VALUES (@cycle, @simunit_id, @x, @y, @z)
                    RETURNING id", connection, transaction)) {

                    using var insertSimunitDataCommand = new NpgsqlCommand(@"
                        INSERT INTO simunit_data (simunit, characteristic, value)
                        VALUES (@simunit, @characteristic, @value)", connection, transaction);

                    var cycleParameter = insertSimunitCommand.Parameters.Add("cycle", NpgsqlTypes.NpgsqlDbType.Bigint);
                    var simunitIdParameter = insertSimunitCommand.Parameters.Add("simunit_id", NpgsqlTypes.NpgsqlDbType.Bigint);
                    var xParameter = insertSimunitCommand.Parameters.Add("x", NpgsqlTypes.NpgsqlDbType.Integer);
                    var yParameter = insertSimunitCommand.Parameters.Add("y", NpgsqlTypes.NpgsqlDbType.Integer);
                    var zParameter = insertSimunitCommand.Parameters.Add("z", NpgsqlTypes.NpgsqlDbType.Integer);

                    var simunitFkParameter = insertSimunitDataCommand.Parameters.Add("simunit", NpgsqlTypes.NpgsqlDbType.Bigint);
                    var characteristicFkParameter = insertSimunitDataCommand.Parameters.Add("characteristic", NpgsqlTypes.NpgsqlDbType.Integer);
                    var valueParameter = insertSimunitDataCommand.Parameters.Add("value", NpgsqlTypes.NpgsqlDbType.Real);

                    cycleParameter.Value = cycleId;

                    foreach (var cellEntry in simulation.GetAllCellsDirect()) {
                        simunitIdParameter.Value = cellEntry.Value.GetId();
                        xParameter.Value = cellEntry.Key.X;
                        yParameter.Value = cellEntry.Key.Y;
                        zParameter.Value = cellEntry.Key.Z;

                        object? simunitRowIdObj = insertSimunitCommand.ExecuteScalar();
                        if (simunitRowIdObj == null) {
                            throw new Exception("Simunit insert failed: no row returned.");
                        }

                        long simunitRowId = Convert.ToInt64(simunitRowIdObj);
                        simunitFkParameter.Value = simunitRowId;

                        for (int characteristicOrd = 0; characteristicOrd < characteristicCount; characteristicOrd++) {
                            characteristicFkParameter.Value = characteristicIdByOrd[characteristicOrd];
                            valueParameter.Value = cellEntry.Value[characteristicOrd];
                            insertSimunitDataCommand.ExecuteNonQuery();
                        }
                    }
                }

                using (var updateWorldCommand = new NpgsqlCommand(@"
                    UPDATE world
                    SET last_cycle = @last_cycle,
                        next_cell_id = @next_cell_id
                    WHERE id = @world_id", connection, transaction)) {

                    updateWorldCommand.Parameters.AddWithValue("last_cycle", cycleNumber);
                    updateWorldCommand.Parameters.AddWithValue("next_cell_id", nextCellId);
                    updateWorldCommand.Parameters.AddWithValue("world_id", worldId);

                    int affectedRows = updateWorldCommand.ExecuteNonQuery();
                    if (affectedRows != 1) {
                        throw new Exception($"World update failed for id={worldId}.");
                    }
                }

                transaction.Commit();
                simulation.World.LastCycle = cycleNumber;
                simulation.World.NextCellId = nextCellId;
                error = null;
                return true;
            } catch (Exception ex) {
                transaction.Rollback();
                error = ex.Message;
                return false;
            }
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    // Get cycle ID based on world ID and cycle number.
    private static bool GetCycleId(
        NpgsqlConnection connection,
        int worldId,
        long cycleNumber,
        out long cycleId,
        out string? error) {

        using var command = new NpgsqlCommand(@"
            SELECT id
            FROM cycle
            WHERE world = @world AND cycle = @cycle", connection);

        command.Parameters.AddWithValue("world", worldId);
        command.Parameters.AddWithValue("cycle", cycleNumber);

        object? cycleIdObj = command.ExecuteScalar();
        if (cycleIdObj == null) {
            cycleId = 0;
            error = $"State for world id={worldId}, cycle={cycleNumber} was not found.";
            return false;
        }

        cycleId = Convert.ToInt64(cycleIdObj);
        error = null;
        return true;
    }

    // Load one simulation state for the specified world and cycle number.
    public bool LoadState(int worldId, long cycleNumber, Simulation simulation, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        if (worldId <= 0) {
            error = $"Invalid world ID '{worldId}'.";
            return false;
        }

        if (cycleNumber < 0) {
            error = $"Invalid cycle number '{cycleNumber}'.";
            return false;
        }

        try {
            using var connection = DataSource!.OpenConnection();
            if (!GetCycleId(connection, worldId, cycleNumber, out long cycleId, out string? cycleError)) {
                error = cycleError;
                return false;
            }

            if (!Cache.WorldId.HasValue || Cache.WorldId.Value != worldId) {
                error = "World definition cache is not ready for this world. Load world definition before loading state.";
                return false;
            }

            if (Cache.CharacteristicIdByOrd.Length != Characteristics.Count) {
                error = "Characteristic cache is not consistent with loaded world definition.";
                return false;
            }

            long nextCellId = simulation.World.NextCellId;

            var characteristicOrdById = new Dictionary<int, int>();
            for (int ord = 0; ord < Cache.CharacteristicIdByOrd.Length; ord++) {
                characteristicOrdById[Cache.CharacteristicIdByOrd[ord]] = ord;
            }

            var cellBySimunitRowId = new Dictionary<long, CellHandle>();
            var cells = new List<CellHandle>();

            using (var simunitCommand = new NpgsqlCommand(@"
                SELECT id, simunit_id, x, y, z
                FROM simunit
                WHERE cycle = @cycle_id
                ORDER BY id", connection)) {
                simunitCommand.Parameters.AddWithValue("cycle_id", cycleId);

                using var simunitReader = simunitCommand.ExecuteReader();
                while (simunitReader.Read()) {
                    long simunitRowId = simunitReader.GetInt64(0);
                    long simunitId = simunitReader.GetInt64(1);
                    int x = simunitReader.GetInt32(2);
                    int y = simunitReader.GetInt32(3);
                    int z = simunitReader.GetInt32(4);

                    var cell = new Cell();
                    cell.SetId(simunitId);

                    var cellHandle = new CellHandle(new SimulatorPosition(x, y, z), cell);
                    cellBySimunitRowId[simunitRowId] = cellHandle;
                    cells.Add(cellHandle);
                }
            }

            using (var simunitDataCommand = new NpgsqlCommand(@"
                SELECT sd.simunit, sd.characteristic, sd.value
                FROM simunit_data sd
                INNER JOIN simunit s ON s.id = sd.simunit
                WHERE s.cycle = @cycle_id
                ORDER BY sd.simunit", connection)) {
                simunitDataCommand.Parameters.AddWithValue("cycle_id", cycleId);

                using var simunitDataReader = simunitDataCommand.ExecuteReader();
                while (simunitDataReader.Read()) {
                    long simunitRowId = simunitDataReader.GetInt64(0);
                    int characteristicId = simunitDataReader.GetInt32(1);
                    float characteristicValue = simunitDataReader.GetFloat(2);

                    if (!cellBySimunitRowId.TryGetValue(simunitRowId, out CellHandle? cellHandle)) {
                        error = $"Unknown simunit row id '{simunitRowId}' while loading state.";
                        return false;
                    }

                    if (!characteristicOrdById.TryGetValue(characteristicId, out int characteristicOrd)) {
                        error = $"Unknown characteristic id '{characteristicId}' while loading state.";
                        return false;
                    }

                    cellHandle.Cell[characteristicOrd] = characteristicValue;
                }
            }

            if (!simulation.SetCurrentState(cycleNumber, nextCellId, cells, out string? setStateError)) {
                error = setStateError;
                return false;
            }

            simulation.World.LastCycle = cycleNumber;
            simulation.World.NextCellId = nextCellId;
            error = null;
            return true;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }
}
