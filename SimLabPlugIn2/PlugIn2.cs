using SimLabApi;

namespace SimLabPlugIn2;

public class PlugIn2 {
    private static string[] _initializationParameters = [];
    private static string[] _updateParameters = [];
    private static string[] _selectionParameters = [];
    private static string[] _reproductionParameters = [];

    private static string[] ReadParameters(string simulationPhase, ISimLabApi api) {
        string[] parameters = api.GetPlugInMethodParameters(simulationPhase);

        if (parameters.Length == 0)
            Console.WriteLine($"    [Plug-in] Simulation phase '{simulationPhase}': no parameters.");
        else
            Console.WriteLine($"    [Plug-in] Simulation phase '{simulationPhase}' parameters: {string.Join(", ", parameters)}");

        return parameters;
    }    
    public static void Initialization(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] Simulation initialization...");
        _initializationParameters = ReadParameters("initialization", api);
        _updateParameters = ReadParameters("update", api);
        _selectionParameters = ReadParameters("selection", api);
        _reproductionParameters = ReadParameters("reproduction", api);

        // first parameter is the path to the file with initial cell configurations
        // each line of the configuration file contains x, y and z coordinates of a cell and 
        // age and size characteristics, separated by commas, e.g.:
        // -1,0,0,1,1
        string configFilePath = _initializationParameters[0];
        string[] lines = System.IO.File.ReadAllLines(configFilePath);

        Console.WriteLine("    [Plug-in] Cell configurations loaded from file '{0}'.", configFilePath);
        foreach (string line in lines) {
            // if a line starts with #, skip it (it is a comment)
            if (line.StartsWith('#'))
                continue;
            string[] characteristics = line.Split(',');
            int x = int.Parse(characteristics[0]);
            int y = int.Parse(characteristics[1]);
            int z = int.Parse(characteristics[2]);
            int age = int.Parse(characteristics[3]);
            int size = int.Parse(characteristics[4]);

            ICellHandle? newCellHandle = api.AddCell(x, y, z);
            if (newCellHandle != null) {
                newCellHandle.Cell["age"] = age;
                newCellHandle.Cell["size"] = size;
                Console.Write($"    [Plug-in] New cell added at {newCellHandle.Position, -15} with ");
                Console.WriteLine($"age={newCellHandle.Cell["age"]}, size={newCellHandle.Cell["size"]}");
            }
        }
    }

    public static void Update(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] Plug-in method Update...");

        // example of using Update parameters
        // ----------------------------------
        // first parameter is "age increment", so, increments the age of all cells by value of the first parameter
        // if the parameter is not provided or is not a valid integer, default increment is 1
        int ageIncrement = 1; // default age increment
        if (_updateParameters.Length > 1 && int.TryParse(_updateParameters[0], out int parsedIncrement)) {
            ageIncrement = parsedIncrement;
        }
        // second parameter is "size increment", so, increments the size of all cells by value of the second parameter
        // if the parameter is not provided or is not a valid integer, default increment is 0 (no increment)
        int sizeIncrement = 0; // default size increment
        if (_updateParameters.Length > 1 && int.TryParse(_updateParameters[1], out int parsedSizeIncrement)) {
            sizeIncrement = parsedSizeIncrement;
        }


        foreach (var cellHandle in api.GetAllCells()) {
            cellHandle.Cell["age"] += ageIncrement;
            cellHandle.Cell["size"] += sizeIncrement;
            Console.WriteLine($"    [Plug-in] Cell at {cellHandle.Position, -15} : Age updated to {cellHandle.Cell["age"]}, Size updated to {cellHandle.Cell["size"]}");
        }
    }

    public static void Selection(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] Plug-in method Selection...");
        // if cell age is greater than a certain value (provided as the first parameter), 
        // remove the cell from the simulation
        // if the parameter is not provided or is not a valid integer, default age threshold is 35
        int ageThreshold = 35; // default age threshold
        if (_selectionParameters.Length > 0 && int.TryParse(_selectionParameters[0], out int parsedAgeThreshold)) {
            ageThreshold = parsedAgeThreshold;
        }
        // we need to collect the cells that will be removed in a separate list, 
        // because we cannot modify the collection of cells while iterating through it
        List<ICellHandle> cellsToRemove = [];
        foreach (var cellHandle in api.GetAllCells()) {
            if (cellHandle.Cell["age"] > ageThreshold) {
                cellsToRemove.Add(cellHandle);
            }
        }

        foreach (var cellHandle in cellsToRemove) {
            api.RemoveCell(cellHandle.Position.X, cellHandle.Position.Y, cellHandle.Position.Z);
            Console.WriteLine($"    [Plug-in] Cell at {cellHandle.Position, -15} removed.");
        }
    }

    public static void Reproduction(ISimLabApi api) {
        Console.WriteLine("    [Plug-in] Plug-in method Reproduction...");

        // if cell size is greater than a certain value (provided as the first parameter),
        // the cell splits and creates a new child cell to the simulation,
        // with age 1 and size equal to half of the size of the parent cell
        // child cell is added to any free neighboring position
        // parent cell size is split in half, so that it can reproduce again in the future if it grows enough

        // if the parameter is not provided or is not a valid integer, default size threshold is 10
        int sizeThreshold = 10; // default size threshold
        if (_reproductionParameters.Length > 0 && int.TryParse(_reproductionParameters[0], out int parsedSizeThreshold)) {
            sizeThreshold = parsedSizeThreshold;
        }

        // we need to collect the cells that will reproduce in a separate list, 
        // because we cannot modify the collection of cells while iterating through it
        List<ICellHandle> cellsToReproduce = [];
        foreach (var cellHandle in api.GetAllCells()) {
            if (cellHandle.Cell["size"] > sizeThreshold) {
                cellsToReproduce.Add(cellHandle);
            }
        }
        foreach (var cellHandle in cellsToReproduce) {
            // child cell is added at any free neighboring position
            // loop at neighboring positions, including diagonals, and 
            // try to add the second child cell at the first free position
            var directions = new (int dx, int dy)[] { (1, 0), (0, 1), (-1, 0), (0, -1), (1, 1), (-1, 1), (-1, -1), (1, -1) };
            foreach (var (dx, dy) in directions) {
                int newX = cellHandle.Position.X + dx;
                int newY = cellHandle.Position.Y + dy;
                int newZ = cellHandle.Position.Z;
                if (api.TryGetCell(newX, newY, newZ) == null) { // if there is no cell at this position
                    ICellHandle? childCellHandle = api.AddCell(newX, newY, newZ);
                    if (childCellHandle != null) {
                        childCellHandle.Cell["age"] = 1;
                        childCellHandle.Cell["size"] = cellHandle.Cell["size"] / 2;
                        Console.WriteLine($"    [Plug-in] Child cell added at {childCellHandle.Position, -15} with age={childCellHandle.Cell["age"]}, size={childCellHandle.Cell["size"]}");
                    }
                    break; // stop after adding child cell
                }
            }

            // modify size of the parent cell
            cellHandle.Cell["size"] = cellHandle.Cell["size"] / 2;
            Console.WriteLine($"    [Plug-in] Cell at {cellHandle.Position, -15} : Size updated to {cellHandle.Cell["size"]} after splitting.");
        }   
    }

    // TODO: This is just a test method. Remove later.
    public static void Test(ISimLabApi api) {
        Console.WriteLine("Hello from plug-in method Test.");
        api.Test("plug-in");
    }
}
