using SimLabApi;

namespace SimLabPlugIn;

public class PlugIn {
    public static void Update(ISimLabApi api) {
        Console.WriteLine("Hello from plug-in method Update.");
        api.Test("plug-in");
    }
}
