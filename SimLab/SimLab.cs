using SimLab.Cmd;
using Npgsql;

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


    // public static void Main(string[] args) {
    public static async Task Main(string[] args) {
        static string ReadPassword() {
            var password = "";
            ConsoleKey key;

            do {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && password.Length > 0) {
                    password = password[..^1];
                } else if (!char.IsControl(keyInfo.KeyChar)) {
                    password += keyInfo.KeyChar;
                }
            }
            while (key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }

        Console.Write("Enter password for user 'simlab': ");
        string password = ReadPassword();
        var connectionString = $"Host=localhost;Port=5432;Database=simlab;Username=simlab;Password={password}";
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT current_user, current_database()", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync()) {
            Console.WriteLine($"User: {reader.GetString(0)}");
            Console.WriteLine($"Database: {reader.GetString(1)}");
        }


        if (args.Length <= 1) {
            Controller controller = new(args);
            controller.Run();
        } else {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage: simlab [configuration-file-name]");
        }
    }
}
