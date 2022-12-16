using System;
using System.Collections;
using System.Collections.Generic;

namespace specify_client.data
{
    public static partial class Cache
    {
        [NonSerialized]
        public const int AF_INET = 2;    // IP_v4 = System.Net.Sockets.AddressFamily.InterNetwork
        [NonSerialized]
        public const int AF_INET6 = 23; // IP_v6 = System.Net.Sockets.AddressFamily.InterNetworkV6
        public static List<string> Issues { get; set; }
        public static Dictionary<string, object> Os { get; private set; }
        public static Dictionary<string, object> Cs { get; private set; }
        public static IDictionary SystemVariables { get; private set; }
        public static IDictionary UserVariables { get; private set; }
        public static List<OutputProcess> RunningProcesses { get; private set; }
        public static List<Dictionary<string, object>> Services { get; private set; }
        public static List<InstalledApp> InstalledApps { get; private set; }
        public static List<Dictionary<string, object>> InstalledHotfixes { get; private set; }
        public static List<ScheduledTask> ScheduledTasks { get; private set; }
        public static List<string> AvList { get; private set; }
        public static List<string> FwList { get; private set; }
        public static string HostsFile { get; private set; }
        public static bool? UacEnabled { get; private set; }
        public static int? UacLevel { get; private set; }
        public static List<Dictionary<string, object>> NetAdapters { get; private set; }
        public static List<Dictionary<string, object>> IPRoutes { get; private set; }
        public static List<NetworkConnection> NetworkConnections { get; private set; }

        public static string Username => Environment.UserName;
        // all the hardware stuff
        //each item in the list is a stick of ram
        public static List<RamStick> Ram { get; private set; }
        public static List<DiskDrive> Disks { get; private set; }
        public static Dictionary<string, object> Cpu { get; private set; }
        public static List<Dictionary<string, object>> Gpu { get; private set; }
        public static Dictionary<string, object> Motherboard { get; private set; }
        public static List<Dictionary<string, object>> AudioDevices { get; private set; }
        public static Dictionary<string, object> Tpm { get; private set; }
        public static List<Dictionary<string, object>> Drivers { get; private set; }
        public static List<Dictionary<string, object>> Devices { get; private set; }
        public static List<TempMeasurement> Temperatures { get; private set; }
        public static List<BatteryData> Batteries { get; private set; }
        public static bool? SecureBootEnabled { get; private set; }
        public static List<IRegistryValue> ChoiceRegistryValues { get; private set; }
        public static List<Monitor> MonitorInfo { get; private set; }

        private static readonly List<string> SystemProcesses = new List<string>()
    {
        "Memory Compression",
        "Registry",
        "System",
        "Idle"
    };
    }
}