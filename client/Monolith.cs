using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Net;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using specify_client.data;

namespace specify_client;

/**
 * The big structure of all the things
 */
[Serializable]
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
    /** For issues with gathering the data itself. No diagnoses based on the info will be made in this program. */
    public List<string> Issues;

    public Monolith()
    {
        Version = Program.SpecifyVersion;
        Meta = new MonolithMeta
        {
            ElapsedTime = Program.Time.ElapsedMilliseconds
        };
        BasicInfo = new MonolithBasicInfo();
        System = new MonolithSystem();
        Hardware = new MonolithHardware();
        Security = new MonolithSecurity();
        Network = new MonolithNetwork();
        Issues = data.Cache.Issues;
    }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented) + Environment.NewLine;
    }

    public static void WriteFile()
    {
        Program.Time.Stop();
        MonolithCache.Monolith.Meta.GenerationDate = DateTime.Now;

        var serialized = MonolithCache.Monolith.Serialize();

        if (Settings.RedactUsername)
        {
            serialized = serialized.Replace(data.Cache.Username, "[REDACTED]");
        }

        if (!Settings.DontUpload)
        {
            File.WriteAllText("specify_specs.json", serialized);
        }
    }

    private static void CacheError(object thing)
    {
        throw new Exception("MonolithCache item doesn't exist: " + nameof(thing));
    }
}

public struct MonolithMeta
{
    public long ElapsedTime;
    public DateTime GenerationDate;
}

[Serializable]
public class MonolithBasicInfo
{
    public string Edition;
    public string Version;
    public string FriendlyVersion;
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
        var os = data.Cache.Os;
        //win32 computersystem wmi class
        var cs = data.Cache.Cs;

        Edition = (string)os["Caption"];
        Version = (string)os["Version"];
        FriendlyVersion = Utils.GetRegistryValue<string>(Registry.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "DisplayVersion");
        InstallDate = Utils.CimToIsoDate((string)os["InstallDate"]);
        Uptime = (DateTime.Now - ManagementDateTimeConverter.ToDateTime((string)os["LastBootUpTime"]))
            .ToString("g");
        Hostname = Dns.GetHostName();
        Username = data.Cache.Username;
        Domain = Environment.GetEnvironmentVariable("userdomain");
        BootMode = Environment.GetEnvironmentVariable("firmware_type");
        BootState = (string)cs["BootupState"];
    }
}

[Serializable]
public class MonolithSecurity
{
    public List<string> AvList;
    public List<string> FwList;
    public bool? UacEnabled;
    public bool? SecureBootEnabled;
    public int? UacLevel;
    public Dictionary<string, object> Tpm;

    public MonolithSecurity()
    {
        AvList = data.Cache.AvList;
        FwList = data.Cache.FwList;
        UacEnabled = data.Cache.UacEnabled;
        SecureBootEnabled = data.Cache.SecureBootEnabled;
        Tpm = data.Cache.Tpm;
        UacLevel = data.Cache.UacLevel;
    }
}

[Serializable]
public class MonolithHardware
{
    public List<RamStick> Ram;
    public Dictionary<string, object> Cpu;
    public List<Dictionary<string, object>> Gpu;
    public Dictionary<string, object> Motherboard;
    public List<Dictionary<string, object>> AudioDevices;
    public List<Monitor> Monitors;
    public List<Dictionary<string, object>> Drivers;
    public List<Dictionary<string, object>> Devices;
    public List<DiskDrive> Storage;
    public List<TempMeasurement> Temperatures;
    public List<BatteryData> Batteries;

    public MonolithHardware()
    {
        Ram = data.Cache.Ram;
        Cpu = data.Cache.Cpu;
        Gpu = data.Cache.Gpu;
        Motherboard = data.Cache.Motherboard;
        AudioDevices = data.Cache.AudioDevices;
        Monitors = data.Cache.MonitorInfo;
        Drivers = data.Cache.Drivers;
        Devices = data.Cache.Devices;
        Storage = data.Cache.Disks;
        Temperatures = data.Cache.Temperatures;
        Batteries = data.Cache.Batteries;
    }
}

[Serializable]
public class MonolithSystem
{
    public IDictionary UserVariables;
    public IDictionary SystemVariables;
    public List<OutputProcess> RunningProcesses;
    public List<Dictionary<string, object>> Services;
    public List<Dictionary<string, object>> InstalledApps;
    public List<Dictionary<string, object>> InstalledHotfixes;
    public List<ScheduledTask> ScheduledTasks;
    public List<IRegistryValue> ChoiceRegistryValues;

    public MonolithSystem()
    {
        UserVariables = data.Cache.UserVariables;
        SystemVariables = data.Cache.SystemVariables;
        RunningProcesses = data.Cache.RunningProcesses;
        Services = data.Cache.Services;
        InstalledApps = data.Cache.InstalledApps;
        InstalledHotfixes = data.Cache.InstalledHotfixes;
        ScheduledTasks = data.Cache.ScheduledTasks;
        ChoiceRegistryValues = data.Cache.ChoiceRegistryValues;
    }
}

[Serializable]
public class MonolithNetwork
{
    public List<Dictionary<string, object>> Adapters;
    public List<Dictionary<string, object>> Routes;
    public List<NetworkConnection> NetworkConnections;
    public string HostsFile;

    public MonolithNetwork()
    {
        Adapters = data.Cache.NetAdapters;
        Routes = data.Cache.IPRoutes;
        NetworkConnections = data.Cache.NetworkConnections;
        HostsFile = data.Cache.HostsFile;
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