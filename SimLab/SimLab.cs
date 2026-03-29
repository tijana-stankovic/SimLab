using SimLab.Cmd;
using SimLab.DB;

namespace SimLab;

/// <summary>
/// Represents the SimLab application entry point.
/// This class provides the Main() method, which is the starting point of the application.
/// </summary>
internal class SimLab {

    /// <summary>
    /// The starting point of the application.
    /// This method is called when the program starts.
    /// It initializes the controller part of the application and passes control to the controller.
    /// Only one command line parameter is supported:
    /// 'configuration-file-name', which is the name of the SimLab configuration file.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the program.</param>
    public static void Main(string[] args) {
        string password = Cli.ReadPassword("Enter password for user 'simlab': ");
        string connectionString = $"Host=localhost;Port=5432;Database=simlab;Username=simlab;Password={password}";
        IDatabase database = DatabaseSelector.Select(DatabaseType.PostgreSql, connectionString);

        if (database.Connect(out string? error)) {
            if (database.TryGetConnectionInfo(out string? userName, out string? databaseName, out error)) {
                Console.WriteLine($"User: {userName}");
                Console.WriteLine($"Database: {databaseName}");
            } else {
                Console.WriteLine("Connected to database.");
            }
        } else {
            Console.Error.WriteLine($"Database connection failed: {error}");
        }

        if (args.Length <= 1) {
            try {
                Controller controller = new(args);
                controller.Run();
            } finally {
                database.Disconnect();
            }
        } else {
            database.Disconnect();
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage: simlab [configuration-file-name]");
        }
    }
}
