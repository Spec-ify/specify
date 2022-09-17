using Microsoft.Management.Infrastructure;

namespace specify_client;

public class Program
{
    static void Main()
    {
        var d = new Data();

        var e = d.GetWmi();

        foreach (CimProperty p in e)
        {
            Console.WriteLine("{0} = {1}", p.Name, p.Value);
        }
    }
}

