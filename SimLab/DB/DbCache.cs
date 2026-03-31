namespace SimLab.DB;

internal class DbCache {
    public int? WorldId { get; private set; } = null;

    // Key: Characteristic name (case-insensitive), Value: Characteristic ID
    // this is used to quickly look up characteristic DB IDs by their names without querying the database repeatedly
    public Dictionary<string, int> CharacteristicIdByName { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    // Index: Characteristic ordinal (0-based), Value: Characteristic ID
    // this is used to quickly look up characteristic DB IDs by their ordinals without querying the database repeatedly
    public int[] CharacteristicIdByOrd { get; private set; } = [];

    public void Clear() {
        WorldId = null;
        CharacteristicIdByName.Clear();
        CharacteristicIdByOrd = [];
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
}
