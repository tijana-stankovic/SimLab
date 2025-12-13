using SimLabAPI;

namespace SimLabPlugIn;

public class PlugIn {
    public static void Update(ISimLabAPI api) {
        api.Test("Hello from PlugIn!");
    }
}
