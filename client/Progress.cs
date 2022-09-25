using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace specify_client
{
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
        public Action Action { get; set; }
        public List<string> Dependencies { get; set; }

        public ProgressStatus(string name, Action a, List<string> deps = null)
        {
            Name = name;
            Status = ProgressType.Queued;
            Action = a;
            Dependencies = deps ?? new List<string>();
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
                { "DummyTimer", new ProgressStatus("Dummy 5 second timer", DataCache.DummyTimer) },
                { "Test", new ProgressStatus("Test thing", () => Program.PrettyPrintObject(MonolithBasicInfo.Create()), new List<string>(){"MainData"}) }
            };
        }

        public void RunItem(string key)
        {
            var item = Items[key] ?? throw new ArgumentNullException(nameof(key));
            
            new Thread(() =>
            {
                item.Status = ProgressType.Processing;

                foreach (var k in item.Dependencies)
                {
                    var dep = Items[k] ?? throw new Exception("Dependency " + k + " of " + key + " does not exist!");
                    while (dep.Status != ProgressType.Complete)
                    {
                        Thread.Sleep(0);
                    }
                }

                item.Action();
                
                item.Status = ProgressType.Complete;
            }).Start();
        }
    }
}
