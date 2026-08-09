using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace specify_client;

public static class DebugLog
{
    
    public static string LogText;
    public static readonly int[] ErrorCount = new int[7];
    private static bool Started = false;
    private static DateTime LogStartTime { get; set; }
    private const string LogFilePath = "specify_debug.log";
    private const string LogFailureFilePath = "specify_log_failure.log";
    private static readonly bool[] RegionStarted = new bool[6];
    private static readonly bool[] RegionCompleted = new bool[6];
    private static readonly DateTime[] RegionStartTime = new DateTime[6];
    private static ConcurrentDictionary<string, DateTime>[] OpenTasks = new ConcurrentDictionary<string, DateTime>[6];

    private static SemaphoreSlim logSemaphore = new(1, 1);

    public enum Region
    {
        Main = 0,
        System = 1,
        Security = 2,
        Networking = 3,
        Hardware = 4,
        Events = 5,
        Misc = 6
        
    }

    public enum EventType
    {
        REGION_START = 0,
        INFORMATION = 1,
        WARNING = 2,
        ERROR = 3,
        REGION_END = 4
    }
    public static async Task OpenTask(Region region, string taskName)
    {
        if (!OpenTasks[(int)region].ContainsKey(taskName))
        {
            int timeout = 0;
            while(!OpenTasks[(int)region].TryAdd(taskName, DateTime.Now))
            {
                if(timeout > 10)
                {
                    await LogEventAsync($"{taskName} could not be opened. Unknown error.", region, EventType.ERROR);
                    return;
                }
                await Task.Delay(10);
                timeout++;
            }

            await LogEventAsync($"Task Started: {taskName}", region);
        }
        // Ensure OpenTask hasn't been called twice on the same task.
        else
        {
            await LogEventAsync($"{taskName} has already been started. This is a specify-specific error.", region, EventType.ERROR);
        }
    }
    public static async Task CloseTask(Region region, string taskName)
    {
        if (OpenTasks[(int)region].ContainsKey(taskName))
        {
            var runtime = (DateTime.Now - OpenTasks[(int)region][taskName]).TotalMilliseconds;
            int timeout = 0;
            
            while(!OpenTasks[(int)region].TryRemove(taskName, out _))
            {
                if (timeout > 10)
                {
                    await LogEventAsync($"{taskName} could not be opened. Unknown error.", region, EventType.ERROR);
                    return;
                }
                await Task.Delay(10);
                timeout++;
            }
            await LogEventAsync($"Task Completed: {taskName} - Runtime: {runtime}", region);

        }
        // Ensure CloseTask hasn't been called on a task that was never opened, or called twice on the same task.
        else
        {
            await LogEventAsync($"DebugLog Task could not be closed. Task was not in list. {taskName}", region, EventType.ERROR);
        }
    }
    /// <summary>
    /// Verifies that no tasks remain open and will log errors for each open task. This method should only be run one time: Immediately prior to serialization.
    /// </summary>
    /// <returns></returns>
    public static void CheckOpenTasks()
    {
        for(int i = 0; i < OpenTasks.Count(); i++)
        {
            Region region = (Region)i;
            var section = OpenTasks[i];
            if(section.Count > 0)
            {
                LogEvent($"{region} has outstanding tasks:", region, EventType.ERROR);
                foreach(var task in section)
                {
                    LogEvent($"OUTSTANDING: {task.Key}", region);
                }
            }
        }
    }
    public static async Task StartDebugLog()
    {
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
            OpenTasks[i] = new();
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
        if (region != Region.Misc && (!RegionStarted[(int)region] || RegionCompleted[(int)region]))
        {
            debugString = CreateDebugString($"Logging attempted on uninitialized region - {message}", region, EventType.ERROR);
        }

        if (Settings.EnableDebug)
        {
            await logSemaphore.WaitAsync();
            int retryCount = 0;
            while (true)
            {
                try
                {
                    var writer = new StreamWriter(LogFilePath, true);
                    await writer.WriteAsync(debugString);
                    writer.Close();
                    break;
                }
                catch (Exception ex) 
                {
                    if(retryCount > 10)
                    {
                        File.WriteAllText(LogFailureFilePath, ex.ToString());
                        Settings.EnableDebug = false;
                        break;
                    }
                    await Task.Delay(30);
                    retryCount++;
                    continue;
                }
            }
            logSemaphore.Release();
        }
        
        if (Settings.Headless)
            Console.WriteLine(debugString);
        
        LogText += debugString;
    }

    public static void LogEvent(string message, Region region = Region.Misc, EventType type = EventType.INFORMATION)
    {
        if (!Started)
        {
            return;
        }
        string debugString = CreateDebugString(message, region, type);
        if (region != Region.Misc && (!RegionStarted[(int)region] || RegionCompleted[(int)region]))
        {
            debugString = CreateDebugString($"Logging attempted on uninitialized region - {message}", region, EventType.ERROR);
        }
        if (Settings.EnableDebug)
        {
            logSemaphore.Wait();
            int retryCount = 0;
            while (true)
            {
                try
                {
                    var writer = new StreamWriter(LogFilePath, true);
                    writer.Write(debugString);
                    writer.Close();
                    break;
                }
                catch (Exception ex)
                {
                    if (retryCount > 10)
                    {
                        File.WriteAllText(LogFailureFilePath, ex.ToString());
                        Settings.EnableDebug = false;
                        break;
                    }
                    Thread.Sleep(30);
                    retryCount++;
                    continue;
                }
            }
            logSemaphore.Release();
        }

        if (Settings.Headless)
            Console.WriteLine(debugString);

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

        Monolith.ProgramDone(ProgramDoneState.ProgramFailed);
    }
}