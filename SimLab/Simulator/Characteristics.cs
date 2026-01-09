namespace SimLab.Simulator;

internal class Characteristics {
    private static readonly Dictionary<string, int> _indexByName = new(StringComparer.OrdinalIgnoreCase);

    public static int Count { get; set; } = 0;

    public static void Init(string listOfCharacteristics){
        IEnumerable<string> names = listOfCharacteristics.Split(',').Select(s => s.Trim());

        int i = 0;
        foreach (var name in names) {
            _indexByName[name.Trim()] = i++;
        }

        Count = _indexByName.Count;
    }

    public static int GetIndex(string name) {
        return _indexByName[name]; // throw an exception if it doesn't exist
    }

    public static bool TryGetIndex(string name, out int index) {
        return _indexByName.TryGetValue(name, out index);
    }
}
