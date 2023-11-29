using Newtonsoft.Json;

namespace Tests;
internal class UnitTestUtils
{
    public static void PrintProperties(object? obj)
    {
        Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
    }
}
