using System.Management;
using Newtonsoft.Json;

namespace specify_client;

public class Program
{
    static void Main()
    {
        //Console.WriteLine("Last boot up time: " + Data.CimToIsoDate((string) DataCache.Os["LastBootUpTime"]));
        //Console.WriteLine("Time now: " + Data.DateTimeToIsoDate(DateTime.Now));
        PrettyPrintObject(MonolithBasicInfo.Create());
    }

    /**
     * System.Text.Json doesn't work, so I'm using Newtonsoft.Json
     */
    public static void PrettyPrintObject(object o)
    {
        var jsonString = JsonConvert.SerializeObject(o, Formatting.Indented);

        Console.WriteLine(jsonString);
    }
}
