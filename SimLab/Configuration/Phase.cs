using SimLabApi;

namespace SimLab.Configuration;

internal static class Phase {
    // for conversion between enum and string values
    private static readonly Dictionary<PhaseName, string> Names = new() {
        { PhaseName.Initialization, "Initialization" },
        { PhaseName.PreCycle, "PreCycle" },
        { PhaseName.ProcessWorld, "ProcessWorld" },
        { PhaseName.Update, "Update" },
        { PhaseName.Evaluation, "Evaluation" },
        { PhaseName.Reproduction, "Reproduction" },
        { PhaseName.Selection, "Selection" },
        { PhaseName.PostCycle, "PostCycle" }
    };

    // for conversion between string and enum values, case-insensitive
    private static readonly Dictionary<string, PhaseName> ValuesByName =
        Names.ToDictionary(
            pair => pair.Value,
            pair => pair.Key,
            StringComparer.OrdinalIgnoreCase);

    public static string ToText(PhaseName phaseName) {
        return Names[phaseName];
    }

    public static bool TryToValue(string? text, out PhaseName phaseName) {
        if (string.IsNullOrWhiteSpace(text)) {
            phaseName = PhaseName.Initialization;
            return false;
        }

        return ValuesByName.TryGetValue(text.Trim(), out phaseName);
    }
}
