using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace specify_client;

public class Program
{
    public const string SpecifyVersion = "v1.1.1";
    public static Stopwatch Time;

    public static async Task Main()
    {
        try
        {
            data.Cache.Issues = new List<string>();
            var initialConsoleFg = Console.ForegroundColor;
            var initialConsoleBg = Console.BackgroundColor;

            Time = new Stopwatch();
            Time.Start();
            await DebugLog.StartDebugLog();
            var pList = new ProgressList();
            pList.RunItem("MainData");
            pList.RunItem("SystemData");
            pList.RunItem("Security");
            pList.RunItem("Network");
            pList.RunItem("Hardware");
            pList.RunItem(ProgressList.Specificializing);
        }
        catch (Exception ex)
        {
            await DebugLog.LogFatalError(ex.ToString(), DebugLog.Region.Misc);
        }

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