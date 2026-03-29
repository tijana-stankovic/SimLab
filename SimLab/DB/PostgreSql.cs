using Npgsql;
using SimLab.Simulator;

namespace SimLab.DB;

internal class PostgreSql(string connectionString) : IDatabase {
    private string ConnectionString { get; } = connectionString;
    private NpgsqlDataSource? DataSource { get; set; }

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

    public bool TryGetConnectionInfo(out string? userName, out string? databaseName, out string? error) {
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

    // TODO: Implement SaveCurrentState method to save simulation state to the database.
    public bool SaveCurrentState(Simulation simulation, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        error = "SaveCurrentState is not implemented yet.";
        return false;
    }

    // TODO: Implement TryLoadState method to load simulation state from the database.
    public bool TryLoadState(long stateId, Simulation simulation, out string? error) {
        if (!IsConnected) {
            error = "Database is not connected.";
            return false;
        }

        error = "TryLoadState is not implemented yet.";
        return false;
    }
}
