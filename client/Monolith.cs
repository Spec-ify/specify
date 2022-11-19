using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Net;
using Newtonsoft.Json;

namespace specify_client
{
    /**
     * The big structure of all the things
     */
    public class Monolith
    {
        // it will say these are never used, but they are serialized
        public string Version;
        public MonolithMeta Meta;
        public MonolithBasicInfo BasicInfo;
        public MonolithSystem System;
        public MonolithHardware Hardware;
        public MonolithSecurity Security;
        public MonolithNetwork Network;

        public Monolith()
        {
            Version = Program.SpecifyVersion;
            Meta = new MonolithMeta
            {
                ElapsedTime = Program.time.ElapsedMilliseconds
            };
            BasicInfo = new MonolithBasicInfo();
            System = new MonolithSystem();
            Hardware = new MonolithHardware();
            Security = new MonolithSecurity();
            Network = new MonolithNetwork();
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented) + Environment.NewLine;
        }

        public static void WriteFile()
        {
            Program.time.Stop();

            var serialized = MonolithCache.Monolith.Serialize();

            if (Settings.RedactUsername)
            {
                serialized = serialized.Replace(DataCache.Username, "[REDACTED]");
            }

            File.WriteAllText("specify_specs.json", serialized);
        }

        private static void CacheError(object thing)
        {
            throw new Exception("MonolithCache item doesn't exist: " + nameof(thing));
        }
    }

    public struct MonolithMeta
    {
        public long ElapsedTime;
    }

    public class MonolithBasicInfo
    {
        public string Edition;
        public string Version;
        public string InstallDate;
        public string Uptime;
        public string Hostname;
        public string Username;
        public string Domain;
        public string BootMode;
        public string BootState;

        public MonolithBasicInfo()
        {
            //win32 operating system class
            var os = DataCache.Os;
            //win32 computersystem wim class
            var cs = DataCache.Cs;

            Edition = (string)os["Caption"];
            Version = (string)os["Version"];
            InstallDate = Data.CimToIsoDate((string)os["InstallDate"]);
            Uptime = (DateTime.Now - ManagementDateTimeConverter.ToDateTime((string)os["LastBootUpTime"]))
                .ToString("g");
            Hostname = Dns.GetHostName();
            Username = DataCache.Username;
            Domain = Environment.GetEnvironmentVariable("userdomain");
            BootMode = Environment.GetEnvironmentVariable("firmware_type");
            BootState = (string)cs["BootupState"];
        }
    }

    public class MonolithSecurity
    {
        public List<string> AvList;
        public List<string> FwList;
        public bool UacEnabled;

        public MonolithSecurity()
        {
            AvList = DataCache.AvList;
            FwList = DataCache.FwList;
            UacEnabled = DataCache.UacEnabled;
        }
    }

    public class MonolithHardware
    {
        public List<Dictionary<string, object>> Ram;
        public Dictionary<string, object> Cpu;
        public List<Dictionary<string, object>> Gpu;
        public Dictionary<string, object> Motherboard;
        public MonolithHardware()
        {
            Ram = DataCache.Ram;
            Cpu = DataCache.Cpu;
            Gpu = DataCache.Gpu;
            Motherboard = DataCache.Motherboard;
        }
    }

    public class MonolithSystem
    {
        public IDictionary UserVariables;
        public IDictionary SystemVariables;
        public List<OutputProcess> RunningProcesses;
        public List<Dictionary<string, object>> Services;
        public List<Dictionary<string, object>> InstalledApps;
        public string HostsFile;

        public MonolithSystem()
        {
            UserVariables = DataCache.UserVariables;
            SystemVariables = DataCache.SystemVariables;
            RunningProcesses = DataCache.RunningProcesses;
            Services = DataCache.Services;
            InstalledApps = DataCache.InstalledApps;
            HostsFile = DataCache.HostsFile;
        }
    }

    public class MonolithNetwork
    {
        public List<Dictionary<string, object>> Adapters;
        public List<Dictionary<string, object>> Routes;

        public MonolithNetwork()
        {
            Adapters = DataCache.NetAdapters;
            Routes = DataCache.IPRoutes;
        }
    }

    public static class MonolithCache
    {
        public static Monolith Monolith { get; set; }

        public static void AssembleCache()
        {
            Monolith = new Monolith();
        }
    }
}
