using SimLabApi;

namespace SimLab.ApiImplementation;

public class API : ISimLabApi {
    public void Test(string callOrigin) {
        Console.WriteLine($"Hello from API method Test (call from {callOrigin}).");
    }
}

