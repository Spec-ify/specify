using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace specify_client
{
    public class Program
    {
        static void Main()
        {
            var pList = new ProgressList();
            pList.RunItem("Test");
            pList.RunItem("MainData");
            
            //Console.WriteLine("Last boot up time: " + Data.CimToIsoDate((string) DataCache.Os["LastBootUpTime"]));
            //Console.WriteLine("Time now: " + Data.DateTimeToIsoDate(DateTime.Now));
            //PrettyPrintObject(MonolithBasicInfo.Create());
        }

        /**
     * System.Text.Json doesn't work, so I'm using Newtonsoft.Json
     */
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
}
