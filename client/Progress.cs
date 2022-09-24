using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

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
    public string Name { get; }
    public ProgressType Status { get; set; }
    public Action<Action> Action { get; set; }

    public SolidColorBrush StatusColor => Status switch
    {
        ProgressType.Queued => (SolidColorBrush)new BrushConverter().ConvertFrom("#b48ead"),
        ProgressType.Processing => (SolidColorBrush)new BrushConverter().ConvertFrom("#88c0d0"),
        ProgressType.Complete => (SolidColorBrush)new BrushConverter().ConvertFrom("#a3be8c"),
        ProgressType.Failed => (SolidColorBrush)new BrushConverter().ConvertFrom("#bf616a"),
        _ => throw new Exception("Bad progress status!")
    };

    public ProgressStatus(string name, Action<Action> a)
    {
        Name = name;
        Status = ProgressType.Queued;
        Action = a;
    }
}

/**
 * Things for progress, will be called by the GUI
 */
public class ProgressList
{
    public Dictionary<string, ProgressStatus> Items { get; set; }

    public ProgressList()
    {
        Items = new Dictionary<string, ProgressStatus>(){
            { "MainData", new ProgressStatus("Main Data", DataCache.MakeMainData) },
            { "DummyTimer", new ProgressStatus("Dummy 5 second timer", DataCache.DummyTimer) }
        };
    }

    public void RunItem(string key)
    {
        var item = Items[key];
        item.Status = ProgressType.Processing;
        item.Action(() =>
        {
            item.Status = ProgressType.Complete;
            Console.WriteLine(key + " is now " + Items[key].Status);
        });
    }
}
