using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimLab.Configuration;

internal static class DatabaseConfigJson {
    static readonly JsonSerializerOptions options = new() {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static bool LoadConfiguration(string configFilePath, out DatabaseCfg? config, out string? error) {
        config = null;

        if (!File.Exists(configFilePath)) {
            error = $"Database configuration file does not exist: {configFilePath}";
            return false;
        }

        string json;
        try {
            json = File.ReadAllText(configFilePath, Encoding.UTF8);
        } catch (Exception ex) {
            error = $"Cannot read database configuration file: {ex.Message}";
            return false;
        }

        try {
            config = JsonSerializer.Deserialize<DatabaseCfg>(json, options);
        } catch (JsonException ex) {
            error = $"Database configuration JSON parse error: {ex.Message}";
            return false;
        } catch (Exception ex) {
            error = $"Unexpected error during database configuration deserialization: {ex.Message}";
            return false;
        }

        if (config == null) {
            error = "Database configuration is empty.";
            return false;
        }

        if (!Validate(config, out error)) {
            return false;
        }

        error = null;
        return true;
    }

    private static bool Validate(DatabaseCfg config, out string? error) {
        List<string> missingProperties = [];

        if (string.IsNullOrWhiteSpace(config.Type)) {
            missingProperties.Add("type");
        }

        if (string.IsNullOrWhiteSpace(config.Host)) {
            missingProperties.Add("host");
        }

        if (!config.Port.HasValue) {
            missingProperties.Add("port");
        }

        if (string.IsNullOrWhiteSpace(config.Database)) {
            missingProperties.Add("database");
        }

        if (string.IsNullOrWhiteSpace(config.User)) {
            missingProperties.Add("user");
        }

        if (missingProperties.Count > 0) {
            error = "Missing required database configuration parameter(s): " + string.Join(", ", missingProperties);
            return false;
        }

        if (config.Port!.Value <= 0 || config.Port.Value > 65535) {
            error = $"Invalid port '{config.Port.Value}'. Valid range is 1-65535.";
            return false;
        }

        error = null;
        return true;
    }
}
