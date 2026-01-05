using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimLab.Configuration;

internal class Json {

    // Serializer options (make deserialize more flexible).
    // Also, fields marked as 'required' must be present in JSON (otherwise JsonException) and
    // unknown properties in JSON are ignored.
    static readonly JsonSerializerOptions options = new() {
            PropertyNameCaseInsensitive = true, // case-insensitive mapping
            ReadCommentHandling = JsonCommentHandling.Skip, // allow comments in JSON
            AllowTrailingCommas = true, // allow trailing comma (after last item in objects/arrays)
            NumberHandling = JsonNumberHandling.AllowReadingFromString // allow reading numbers from strings
        };

    /// <summary>
    /// Reads configuration from a JSON file and maps it to a WorldCfg object.
    /// </summary>
    /// <param name="configFilePath">Path to the JSON configuration file.</param>
    /// <param name="world">Output WorldCfg object if successful; null otherwise.</param>
    /// <returns>True if configuration was successfully loaded; false otherwise.</returns>
    public static bool LoadConfiguration(string configFilePath, out WorldCfg? world) {
        world = null;

        // Checks if the config file exists
        if (!File.Exists(configFilePath)) {
            Console.Error.WriteLine($"[Error] Config file does not exist: {configFilePath}");
            return false;
        }

        string json;
        try {
            // Encoding.UTF8 parameter ensures maximal compatibility
            json = File.ReadAllText(configFilePath, Encoding.UTF8);
        } catch (Exception ex) {
            Console.Error.WriteLine("[Error] Cannot read file: " + ex.Message);
            return false;
        }

        // Deserialization, with catching syntax errors (with line/column display)
        try {
            world = JsonSerializer.Deserialize<WorldCfg>(json, options);
        } catch (JsonException ex) {
            ShowJsonParseErrorWithPointer(json, ex);
            return false;
        } catch (Exception ex) {
            Console.Error.WriteLine("[Error] Unexpected problem during deserialization: " + ex.Message);
            return false;
        }

        // If file contains 'null' or mapping was unsuccessful but no exception occurred
        if (world is null) {
            Console.Error.WriteLine("[Error] Configuration is empty (JSON contains literal 'null' or is not properly mapped).");
            Console.Error.WriteLine("Ensure a valid JSON file with configuration data is provided.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Write syntax error in JSON file with line, column and caret (^) pointing to the error position.
    /// </summary>
    /// <param name="json">The JSON string being parsed.</param>
    /// <param name="jex">The JsonException that occurred during parsing.</param>
    private static void ShowJsonParseErrorWithPointer(string json, JsonException jex) {
        Console.Error.WriteLine($"[JSON error] {jex.Message}");

        if (!string.IsNullOrWhiteSpace(jex.Path))
            Console.Error.WriteLine($"JSON path: {jex.Path}");

        // If required properties are missing, do not show line/column
        bool looksLikeMissingRequired =
            jex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) &&
            // using "propert" to catch both "property" and "properties"
            jex.Message.Contains("propert", StringComparison.OrdinalIgnoreCase);

        if (looksLikeMissingRequired) {
            Console.Error.WriteLine("One or more required properties are missing.");
            return;
        } else {
            if (jex.LineNumber is long line && jex.BytePositionInLine is long col) {
                var (lineError, posError) = BuildCaretPointer(json, line, col);
                if (lineError == "") {
                    return;
                }
                Console.Error.WriteLine($"Line: {line + 1}, column: {col + 1}"); // +1 because line/col are 0-based
                Console.Error.WriteLine(lineError); // line containing error
                Console.Error.WriteLine(posError); // line with '^' pointing to error position
            }
        }        
    }

    /// <summary>
    /// Creates a line of text with error and a caret (^) pointing to the error.
    /// </summary>
    /// <param name="text">The full text.</param>
    /// <param name="line">The line number (0-based).</param>
    /// <param name="col">The column number (0-based).</param>
    /// <returns>A tuple containing the line with error and the caret line.</returns>
    private static (string, string) BuildCaretPointer(string text, long line, long col) {
        // Some basic validation
        if (line < 0 || col < 0)
            return ("", "");

        // Split by lines, remove ending '\r' (Windows CRLF)
        var lines = text.Split('\n');
        string lineError = (line >= 0 && line < lines.Length) ? lines[line].TrimEnd('\r') : "";

        // Change tabs to spaces
        lineError = lineError.Replace("\t", "    ");

        // Build position error line with caret (^)
        int caretPos = Math.Max(0, Math.Min((int)col, lineError.Length));
        string posError = new string(' ', caretPos) + "^";

        return (lineError, posError);
    }
}