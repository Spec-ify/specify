using System.Management;
using Newtonsoft.Json;

namespace specify_client;

public class Program
{
    static void Main()
    {
        PrettyPrintObject(MonolithBasicInfo.Create());
    }

    static void PrettyPrintObject(object o)
    {
        var jsonString = JsonConvert.SerializeObject(o, Formatting.Indented);

        Console.WriteLine(jsonString);
    }
}
