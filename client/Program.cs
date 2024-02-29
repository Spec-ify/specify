using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace specify_client;

public class Program
{
    public const string SpecifyVersion = "v1.3.0";
    public static Stopwatch Time;

    public static async Task Main()
    {
        try
        {
            
            // Set Specify to run in en-US so system messages are printed in English.
            var culture = CultureInfo.CreateSpecificCulture("en-US");

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Time = new Stopwatch();
            Time.Start();
            await DebugLog.StartDebugLog();
            var pList = new ProgressList();
            /*pList.RunItem("MainData");
            pList.RunItem("SystemData");
            pList.RunItem("Security");
            pList.RunItem("Network");
            pList.RunItem("Hardware");
            pList.RunItem("Events");
            pList.RunItem(ProgressList.Specificializing);*/

            List<Task> dataTasks = new()
            {
                Task.Run(data.Cache.MakeMainData),
                Task.Run(data.Cache.MakeSystemData),
                Task.Run(data.Cache.MakeSecurityData),
                Task.Run(data.Cache.MakeNetworkData),
                Task.Run(data.Cache.MakeHardwareData),
                Task.Run(data.Cache.MakeEventData)
            };
            await Task.WhenAll(dataTasks);
            pList.RunItem(ProgressList.Specificializing);
        }
        catch (Exception ex)
        {
            await DebugLog.LogFatalError(ex.ToString(), DebugLog.Region.Misc);
        }

    }
}
