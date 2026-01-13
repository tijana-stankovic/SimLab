using SimLabApi;

namespace SimLabPlugIn;

public class PlugIn {
    public static void Initialization(ISimLabApi api) {
        Console.WriteLine("Simulation initialization...");
        ICellHandle? newCellHandle = api.AddCell(1, 1, 0);
        if (newCellHandle != null) {
            newCellHandle.Cell["type"] = 1;
            newCellHandle.Cell["age"] = 1;
            newCellHandle.Cell["status"] = 1;
        }
        newCellHandle = api.AddCell(1, -1, 0);
        if (newCellHandle != null) {
            newCellHandle.Cell["type"] = 1;
            newCellHandle.Cell["age"] = 10;
            newCellHandle.Cell["status"] = 2;
        }
        newCellHandle = api.AddCell(-1, 1, 0);
        if (newCellHandle != null) {
            newCellHandle.Cell["type"] = 2;
            newCellHandle.Cell["age"] = 100;
            newCellHandle.Cell["status"] = 1;
        }
        newCellHandle = api.AddCell(-1, -1, 0);
        if (newCellHandle != null) {
            newCellHandle.Cell["type"] = 2;
            newCellHandle.Cell["age"] = 1000;
            newCellHandle.Cell["status"] = 2;
        }
    }

    public static void Update(ISimLabApi api) {
        Console.WriteLine("Plug-in method Update...");
        ICellHandle? cellHandle = api.TryGetCell(-1, -1, 0);
        if (cellHandle != null)
            cellHandle.Cell["age"]++;
    }

    // TODO: This is just a test method. Remove later.
    public static void Test(ISimLabApi api) {
        Console.WriteLine("Hello from plug-in method Test.");
        api.Test("plug-in");
    }
}
