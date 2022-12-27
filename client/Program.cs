using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace specify_client;

public class Program
{
    public const string SpecifyVersion = "v0.2";
    public static Stopwatch Time;
    public static void Main()
    {
        data.Cache.Issues = new List<string>();
        var initialConsoleFg = Console.ForegroundColor;
        var initialConsoleBg = Console.BackgroundColor;

        Time = new Stopwatch();
        Time.Start();

        var pList = new ProgressList();
        pList.RunItem("MainData");
        pList.RunItem("SystemData");
        pList.RunItem("Security");
        pList.RunItem("Network");
        pList.RunItem("Hardware");
        pList.RunItem("Specificializing");  
    }


    public static void PrettyPrintObject(object o)
    {
        var jsonString = JsonConvert.SerializeObject(o, Formatting.Indented);

        Console.WriteLine(jsonString);
    }

    public static void PrettyPrintWmiResults(List<Dictionary<string, object>> wmi)
    {
        foreach (var instance in wmi)
        {
            foreach (var pair in instance)
            {
                Console.WriteLine("{0} = {1}", pair.Key, pair.Value);
            }

            if (!instance.Equals(wmi.Last()))
            {
                Console.WriteLine("---------------");
            }
        }
    }
}