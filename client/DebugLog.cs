using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace specify_client;

public static class DebugLog
{
    public const string LogFilePath = "specify_debug.log";
    public static string LogText;
    private static bool Enabled = true;
    private static bool Started = false;
    private static DateTime LogStartTime { get; set; }
    private static int[] ErrorCount = new int[6];
    private static bool[] RegionStarted = new bool[5];
    private static bool[] RegionCompleted = new bool[5];
    private static DateTime[] RegionStartTime = new DateTime[5];

    public enum Region
    {
        Main = 0,
        System = 1,
        Security = 2,
        Networking = 3,
        Hardware = 4,
        Misc = 5
    }

    public enum EventType
    {
        REGION_START = 0,
        INFORMATION = 1,
        WARNING = 2,
        ERROR = 3,
        REGION_END = 4
    }

    public static async Task StartDebugLog()
    {
        /*if(!Settings.EnableDebug)
        {
            return;
        }*/
        LogText = "";
        LogStartTime = DateTime.Now;
        if (!File.Exists(LogFilePath) && Settings.EnableDebug)
        {
            File.Create(LogFilePath).Close();
        }
        else if (Settings.EnableDebug)
        {
            await Task.Run(() => File.WriteAllText(LogFilePath, ""));
        }
        for (int i = 0; i < ErrorCount.Length; i++)
        {
            ErrorCount[i] = 0;
        }
        for (int i = 0; i < RegionStarted.Length; i++)
        {
            RegionStarted[i] = false;
            RegionCompleted[i] = false;
        }
        Started = true;
        await LogEventAsync($"--- DEBUG LOG STARTED {LogStartTime.ToString("HH:mm:ss")} ---");
        await LogSettings();
    }

    public static async Task StopDebugLog()
    {
        /*if(!Settings.EnableDebug)
        {
            return;
        }*/
        for (int i = 0; i < RegionCompleted.Length; i++)
        {
            if (!RegionCompleted[i])
            {
                await LogEventAsync($"Logging completed with unfinished region: {(Region)i}", (Region)i, EventType.ERROR);
            }
        }
        for (int i = 0; i < ErrorCount.Length; i++)
        {
            await LogEventAsync($"{(Region)i} Data Errors: {ErrorCount[i]}");
        }
        await LogEventAsync($"Total Elapsed Time: {(DateTime.Now - LogStartTime).TotalMilliseconds}");
        await LogEventAsync($"--- DEBUG LOG FINISHED {DateTime.Now.ToString("HH:mm:ss")} ---");
        Started = false;
    }

    public static async Task StartRegion(Region region)
    {
        /*if(!Settings.EnableDebug)
        {
            return;
        }*/
        if (RegionStarted[(int)region])
        {
            await LogEventAsync($"{region} Region already started.", region, EventType.ERROR);
            return;
        }
        RegionStarted[(int)region] = true;
        RegionStartTime[(int)region] = DateTime.Now;
        await LogEventAsync($"{region} Region Start", region, EventType.REGION_START);
    }

    public static async Task EndRegion(Region region)
    {
        /*if(!Settings.EnableDebug)
        {
            return;
        }*/
        if (RegionCompleted[(int)region])
        {
            await LogEventAsync($"Region already completed.", region, EventType.ERROR);
            return;
        }
        await LogEventAsync($"{region} Region End - Total Time: {(DateTime.Now - RegionStartTime[(int)region]).TotalMilliseconds}ms", region, EventType.REGION_END);
        RegionCompleted[(int)region] = true;
    }

    public static async Task LogEventAsync(string message, Region region = Region.Misc, EventType type = EventType.INFORMATION)
    {
        if (!Started)
        {
            return;
        }
        string debugString = CreateDebugString(message, region, type);
        if (region != Region.Misc)
        {
            if (!RegionStarted[(int)region] || RegionCompleted[(int)region])
            {
                debugString = CreateDebugString($"Logging attempted on uninitialized region - {message}", region, EventType.ERROR);
            }
        }
        var timeout = 5;
        var currentTime = DateTime.Now;
        while (true)
        {
            if (Settings.EnableDebug)
            {
                try
                {
                    await Task.Run(() => File.AppendAllText(LogFilePath, debugString));
                    break;
                }
                catch
                {
                    await Task.Delay(30);
                    if ((DateTime.Now - currentTime).TotalSeconds > timeout)
                    {
                        Settings.EnableDebug = false;
                        break;
                    }
                    continue;
                }
            }
            else
            {
                break;
            }
        }
        LogText += debugString;
    }

    public static void LogEvent(string message, Region region = Region.Misc, EventType type = EventType.INFORMATION)
    {
        if (!Started)
        {
            return;
        }
        string debugString = CreateDebugString(message, region, type);
        if (region != Region.Misc)
        {
            if (!RegionStarted[(int)region] || RegionCompleted[(int)region])
            {
                debugString = CreateDebugString($"Logging attempted on uninitialized region - {message}", region, EventType.ERROR);
            }
        }
        var timeout = 5;
        var currentTime = DateTime.Now;
        while (true)
        {
            if (Settings.EnableDebug)
            {
                try
                {
                    File.AppendAllText(LogFilePath, debugString);
                    break;
                }
                catch
                {
                    Thread.Sleep(30);
                    if ((DateTime.Now - currentTime).TotalSeconds > timeout)
                    {
                        Settings.EnableDebug = false;
                        break;
                    }
                    continue;
                }
            }
            else
            {
                break;
            }
        }
        LogText += debugString;
    }

    private static string CreateDebugString(string message, Region region, EventType type)
    {
        string debugString = $"[{(DateTime.Now - LogStartTime).TotalMilliseconds}]";
        while (debugString.Length < 12)
        {
            debugString += " ";
        }
        switch (type)
        {
            case EventType.INFORMATION:
                debugString += " [Information] ";
                break;

            case EventType.WARNING:
                debugString += "     [Warning] ";
                break;

            case EventType.ERROR:
                debugString += "       [ERROR] !!! ";
                break;

            case EventType.REGION_START:
                debugString += "[Region Start] --- ";
                break;

            case EventType.REGION_END:
                debugString += "  [Region End] --- ";
                break;
        }
        debugString += message;
        if (type == EventType.ERROR)
        {
            debugString += " !!! ";
            ErrorCount[(int)region]++;
        }
        if (type == EventType.REGION_START || type == EventType.REGION_END)
        {
            debugString += " --- ";
        }
        while (debugString.Length < 90)
        {
            debugString += " ";
        }
        debugString += " :";
        if (region != Region.Misc)
        {
            debugString += $" {region}";
        }
        debugString += "\r\n";
        return debugString;
    }

    private static async Task LogSettings()
    {
        var properties = typeof(Settings).GetProperties();
        foreach (PropertyInfo property in properties)
        {
            await LogEventAsync($"{property.Name}: {property.GetValue(null)}");
        }
    }

    public static async Task LogFatalError(string message, Region region)
    {
        Settings.EnableDebug = true;
        await LogEventAsync("UNEXPECTED FATAL EXCEPTION", region, EventType.ERROR);
        await LogEventAsync(message, region, EventType.ERROR);
        while (true)
        {
            try
            {
                File.WriteAllText(LogFilePath, LogText);
                break;
            }
            catch
            {
                Thread.Sleep(30);
                continue;
            }
        }

        Monolith.ProgramDone(3);
    }
}