using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace specify_client;

public class Program
{
    public const string SpecifyVersion = "v0.2";
    public static Stopwatch Time;
    public static void Main()
    {
        data.Cache.Issues = new List<string>();

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
