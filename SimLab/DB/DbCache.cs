namespace SimLab.DB;

internal class DbCache {
    public int? WorldId { get; private set; } = null;

    // Key: Cell characteristic name (case-insensitive), Value: Cell characteristic ID
    // this is used to quickly look up characteristic DB IDs by their names without querying the database repeatedly
    public Dictionary<string, int> CharacteristicIdByName { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    // Index: Cell characteristic ordinal (0-based), Value: Cell characteristic ID
    // this is used to quickly look up characteristic DB IDs by their ordinals without querying the database repeatedly
    public int[] CharacteristicIdByOrd { get; private set; } = [];

    // Key: Global characteristic name (case-insensitive), Value: Global characteristic ID
    // this is used to quickly look up global characteristic DB IDs by their names without querying the database repeatedly
    public Dictionary<string, int> GlobalIdByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Index: Global characteristic ordinal (0-based), Value: Global characteristic ID
    // this is used to quickly look up global characteristic DB IDs by their ordinals without querying the database repeatedly
    public int[] GlobalIdByOrd { get; private set; } = [];

    public void Clear() {
        WorldId = null;
        CharacteristicIdByName.Clear();
        CharacteristicIdByOrd = [];
        GlobalIdByName.Clear();
        GlobalIdByOrd = [];
    }

    public void SetWorld(int worldId) {
        WorldId = worldId;
    }

    public void SetCharacteristicIdByName(Dictionary<string, int> characteristicIdByName) {
        CharacteristicIdByName.Clear();

        foreach (var pair in characteristicIdByName) {
            CharacteristicIdByName[pair.Key] = pair.Value;
        }
    }

    public void SetCharacteristicIdByOrd(int[] characteristicIdByOrd) {
        CharacteristicIdByOrd = (int[])characteristicIdByOrd.Clone();
    }

    public void SetGlobalIdByName(Dictionary<string, int> globalIdByName) {
        GlobalIdByName.Clear();

        foreach (var pair in globalIdByName) {
            GlobalIdByName[pair.Key] = pair.Value;
        }
    }

    public void SetGlobalIdByOrd(int[] globalIdByOrd) {
        GlobalIdByOrd = (int[])globalIdByOrd.Clone();
    }

    public bool TryGetCharacteristicId(string characteristicName, out int characteristicId) {
        return CharacteristicIdByName.TryGetValue(characteristicName, out characteristicId);
    }

    public bool TryGetCharacteristicId(int characteristicOrd, out int characteristicId) {
        if (characteristicOrd < 0 || characteristicOrd >= CharacteristicIdByOrd.Length) {
            characteristicId = 0;
            return false;
        }

        characteristicId = CharacteristicIdByOrd[characteristicOrd];
        return true;
    }

    public bool TryGetGlobalId(string globalName, out int globalId) {
        return GlobalIdByName.TryGetValue(globalName, out globalId);
    }

    public bool TryGetGlobalId(int globalOrd, out int globalId) {
        if (globalOrd < 0 || globalOrd >= GlobalIdByOrd.Length) {
            globalId = 0;
            return false;
        }

        globalId = GlobalIdByOrd[globalOrd];
        return true;
    }
}
