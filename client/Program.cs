using Newtonsoft.Json;

namespace specify_client;

public class Program
{
    static void Main()
    {
        foreach (var task in Data.GetTsStartupTasks())
        {
            Console.WriteLine(task.Path);
        }
    }

    static void PrettyPrintObject(object o)
    {
        var jsonString = JsonConvert.SerializeObject(o, Formatting.Indented);

        Console.WriteLine(jsonString);
    }
}
