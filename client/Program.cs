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
    static void Main()
    {
        data.Cache.Issues = new List<string>();
        var initialConsoleFg = Console.ForegroundColor;
        var initialConsoleBg = Console.BackgroundColor;
        Console.WriteLine($"Specify {SpecifyVersion}");
        Console.WriteLine("This tool gathers information about your computer.  It does not collect any sensitive information.");
            
        // TODO: make settings better
        while (true)
        {
            Console.Write("[Enter] - continue, [q] - quit, ");
            if (Settings.RedactUsername)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
            }
            Console.Write("[1] - Toggle Redact Username");
            Console.ForegroundColor = initialConsoleFg;
            Console.BackgroundColor = initialConsoleBg;
            Console.Write(" ");
            if (!Settings.DontUpload)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
            }
            Console.Write("[2] - Don't Upload");
            Console.ForegroundColor = initialConsoleFg;
            Console.BackgroundColor = initialConsoleBg;
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Q)
            {
                Console.WriteLine("\nGoodbye!");
                return;
            }
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key is ConsoleKey.D1 or ConsoleKey.NumPad1)
                Settings.RedactUsername = !Settings.RedactUsername;
            if (key.Key is ConsoleKey.D2 or ConsoleKey.NumPad2)
                Settings.DontUpload = !Settings.DontUpload;
                
            Console.Write("\r");
        }

        Console.WriteLine("\n");

        Time = new Stopwatch();
        Time.Start();

        var pList = new ProgressList();
        pList.PrintStatuses();
        pList.RunItem("WriteFile");
        pList.RunItem("MainData");
        //pList.RunItem("DummyTimer");
        pList.RunItem("SystemData");
        pList.RunItem("Security");
        pList.RunItem("Network");
        pList.RunItem("Hardware");
        pList.RunItem("Assemble");
        // pList.RunItem("MainData");
        //Console.WriteLine("Last boot up time: " + Data.CimToIsoDate((string) data.Cache.Os["LastBootUpTime"]));
        //Console.WriteLine("Time now: " + Data.DateTimeToIsoDate(DateTime.Now));
        //PrettyPrintObject(MonolithBasicInfo.Create());
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