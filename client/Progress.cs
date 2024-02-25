using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace specify_client;

public enum ProgressType
{
    Queued,
    Processing,
    Complete,
    Failed
}

public class ProgressStatus
{
    CancellationTokenSource tokenSource = new();
    private ProgressType status;

    public CancellationToken Token => tokenSource.Token;
    public string Name { get; }
    public ProgressType Status { get => status; set { status = value; if (value == ProgressType.Failed || value == ProgressType.Complete) tokenSource.Cancel(); } }
    public Func<Task> Action { get; }
    public List<string> Dependencies { get; }
    public bool SkipProgressWait { get; }

    public ProgressStatus(string name, Func<Task> a, List<string> deps = null, bool skipProgressWait = false)
    {
        Name = name;
        Status = ProgressType.Queued;
        Action = a;
        Dependencies = deps ?? new List<string>();
        SkipProgressWait = skipProgressWait;
    }
}

/**
 * Things for progress, will be called by the GUI
 */

public class ProgressList
{
    public Dictionary<string, ProgressStatus> Items { get; set; }
    public const string Specificializing = "Specificializing";

    public ProgressList()
    {
        Items = new Dictionary<string, ProgressStatus>(){
            { "MainData", new ProgressStatus("Main Data", data.Cache.MakeMainData) },
            { "SystemData", new ProgressStatus("System Data", data.Cache.MakeSystemData) },
            { "Security", new ProgressStatus("Security Info", data.Cache.MakeSecurityData) },
            { "Network", new ProgressStatus("Network Info", data.Cache.MakeNetworkData) },
            { "Hardware", new ProgressStatus("Hardware Info", data.Cache.MakeHardwareData) },
            { "Events", new ProgressStatus("Event Logs", data.Cache.MakeEventData) },
            {
                Specificializing,
                new ProgressStatus(Specificializing, Monolith.Specificialize) 
                    //new List<string>(){ "MainData", "SystemData", "Security", "Network", "Hardware" })
            }
        };
    }

    public void RunItem(string key)
    {
        var item = Items.ContainsKey(key) ? Items[key] : throw new Exception($"Progress item {key} doesn't exist!");

        var t = new Thread(async () =>
        {
            foreach (var k in item.Dependencies)
            {
                var dep = Items.ContainsKey(k) ? Items[k] : throw new Exception($"Dependency {k} of {key} does not exist!");

                dep.Token.WaitHandle.WaitOne();
            }
            item.Status = ProgressType.Processing;
            await item.Action();
            item.Status = ProgressType.Complete;
        });
        t.IsBackground = true;
        if (key.Equals(Specificializing)) t.SetApartmentState(ApartmentState.STA);
        t.Start();
    }

    public void PrintStatuses()
    {
        const int maxKeyLength = 20;

        new Thread(() =>
        {
            //Console.WriteLine();
            bool allComplete;
            var cPos = new List<int>();
            var oldStatus = new List<ProgressType>();

            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items.ElementAt(i).Value;
                Console.Write(item.Name.PadRight(maxKeyLength) + " "); PrintColorType(item.Status);
                Console.WriteLine();
                cPos.Add(Console.CursorTop - 1);
                oldStatus.Add(item.Status);
            }

            do
            {
                allComplete = true;

                for (var i = 0; i < Items.Count; i++)
                {
                    var item = Items.ElementAt(i).Value;
                    if (item.Status != ProgressType.Complete)
                    {
                        allComplete = false;
                    }

                    if (item.Status == oldStatus[i]) continue;

                    Console.SetCursorPosition(0, cPos[i]);
                    ClearCurrentConsoleLine();
                    Console.Write(item.Name.PadRight(maxKeyLength) + " "); PrintColorType(item.Status);
                    Console.WriteLine();
                    oldStatus[i] = item.Status;
                }

                Console.SetCursorPosition(0, cPos.Last() + 1);
                Thread.Sleep(100);
            } while (!allComplete);

            Console.SetCursorPosition(0, cPos.Last() + 1);
        }).Start();
    }

    private static void PrintColorType(ProgressType status)
    {
        var originalColor = Console.ForegroundColor;
        var colorList = new List<ConsoleColor>()
        {
            originalColor,
            ConsoleColor.Yellow,
            ConsoleColor.Green,
            ConsoleColor.Red
        };
        Console.ForegroundColor = colorList[(int)status];
        Console.Write(status);
        Console.ForegroundColor = originalColor;
    }

    /**
     * From https://stackoverflow.com/a/8946847 and a comment
     */

    public static void ClearCurrentConsoleLine()
    {
        var currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.BufferWidth));
        Console.SetCursorPosition(0, currentLineCursor);
    }
}