namespace SimLabAPI;

public class API : ISimLabAPI {
    public void Test(string message) {
        Console.WriteLine("API test method called with message: " + message);
    }
}

