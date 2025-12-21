namespace SimLabAPI;

public class API : ISimLabAPI {
    public void Test(string callOrigin) {
        Console.WriteLine($"Hello from API method Test (call from {callOrigin}).");
    }
}

