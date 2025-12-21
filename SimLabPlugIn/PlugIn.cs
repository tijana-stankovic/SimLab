using SimLabAPI;

namespace SimLabPlugIn;

public class PlugIn {
    public static void Update(ISimLabAPI api) {
        Console.WriteLine("Hello from plug-in method Update.");
        api.Test("plug-in");
    }
}
