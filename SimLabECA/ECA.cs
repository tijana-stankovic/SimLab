using SimLabApi;

namespace SimLabECA;

public class ECA {

    public static void Initialization(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA initialization...");
    }

    public static void PreCycle(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA precycle...");
    }

    public static void ProcessWorld(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] ECA processing world...");
    }
}
