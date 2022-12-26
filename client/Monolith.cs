using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Net;
using Microsoft.Win32;
using Newtonsoft.Json;
using specify_client.data;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        Issues = Cache.Issues;
    }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented) + Environment.NewLine;
    }
    
    public static void Specificialize()
    {
        // const string specifiedUploadDomain = "http://localhost";
        // const string specifiedUploadEndpoint = "specified/upload.php";
        const string specifiedUploadDomain = "https://spec-ify.com";
        const string specifiedUploadEndpoint = "upload.php";
        
        Program.Time.Stop();
        var m = new Monolith();
        m.Meta.GenerationDate = DateTime.Now;
        var serialized = m.Serialize();

        if (Settings.RedactUsername)
        {
            serialized = serialized.Replace(Cache.Username, "[REDACTED]");
        }

        if (Settings.RedactOneDriveCommercial)
        {
            try
            {
                var stringToRedact = (string)Cache.UserVariables["OneDriveCommercial"]; // The path containing the Commercial OneDrive
                stringToRedact = stringToRedact.Replace(@"\", @"\\"); // Changing a single \ to two \\ as that is how it shows up in the generated json
                serialized = serialized.Replace(stringToRedact, "[REDACTED]");
            }
            catch
            {
                m.Issues.Add("Commercial OneDrive redaction failed. This usually happens when Commerical OneDrive is not installed.");
                Settings.RedactOneDriveCommercial = false;
                Specificialize();
                return;
            }
        }

        if (Settings.DontUpload)
        {
            File.WriteAllText("specify_specs.json", serialized);
            return;
        }

        var requestTask = DoRequest(serialized);
        requestTask.Wait();
        var url = requestTask.Result;
        if (url == null) return;
        Clipboard.SetText(url);
        Process.Start(url);
    }
    
    private static async Task<string> DoRequest(string str)
    {
        // const string specifiedUploadDomain = "http://localhost";
        // const string specifiedUploadEndpoint = "specified/upload.php";
        const string specifiedUploadDomain = "https://spec-ify.com";
        const string specifiedUploadEndpoint = "upload.php";
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{specifiedUploadDomain}/{specifiedUploadEndpoint}");
        request.Content = new StringContent(str);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nCould not upload. The file has been saved to specify_specs.json.");
            Console.WriteLine($"Please go to {specifiedUploadDomain} to upload the file manually.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            return null;
        }

        var location = response.Headers.Location.ToString();
        //Console.WriteLine(specifiedUploadDomain + location);
        return specifiedUploadDomain + location;
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
        var os = Cache.Os;
        //win32 computersystem wmi class
        var cs = Cache.Cs;

        Edition = (string)os["Caption"];
        Version = (string)os["Version"];
        FriendlyVersion = Utils.GetRegistryValue<string>(Registry.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "DisplayVersion");
        InstallDate = Utils.CimToIsoDate((string)os["InstallDate"]);
        Uptime = (DateTime.Now - ManagementDateTimeConverter.ToDateTime((string)os["LastBootUpTime"]))
            .ToString("g");
        Hostname = Dns.GetHostName();
        Username = Cache.Username;
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
        AvList = Cache.AvList;
        FwList = Cache.FwList;
        UacEnabled = Cache.UacEnabled;
        SecureBootEnabled = Cache.SecureBootEnabled;
        Tpm = Cache.Tpm;
        UacLevel = Cache.UacLevel;
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
    public List<Dictionary<string, object>> BiosInfo;
    public List<DiskDrive> Storage;
    public List<TempMeasurement> Temperatures;
    public List<BatteryData> Batteries;

    public MonolithHardware()
    {
        Ram = Cache.Ram;
        Cpu = Cache.Cpu;
        Gpu = Cache.Gpu;
        Motherboard = Cache.Motherboard;
        AudioDevices = Cache.AudioDevices;
        Monitors = Cache.MonitorInfo;
        Drivers = Cache.Drivers;
        Devices = Cache.Devices;
        BiosInfo = Cache.BiosInfo;
        Storage = Cache.Disks;
        Temperatures = Cache.Temperatures;
        Batteries = Cache.Batteries;
    }
}

[Serializable]
public class MonolithSystem
{
    public IDictionary UserVariables;
    public IDictionary SystemVariables;
    public List<OutputProcess> RunningProcesses;
    public List<Dictionary<string, object>> Services;
    public List<InstalledApp> InstalledApps;
    public List<Dictionary<string, object>> InstalledHotfixes;
    public List<ScheduledTask> ScheduledTasks;
    public List<Dictionary<string, object>> PowerProfiles;
    public List<string> MicroCodes;
    public int RecentMinidumps;
    public bool? StaticCoreCount;
    public List<IRegistryValue> ChoiceRegistryValues;
    public bool? UsernameSpecialCharacters;
    public int? OneDriveCommercialPathLength;
    public int? OneDriveCommercialNameLength;
    public List<Browser> BrowserExtensions;
    public string DefaultBrowser;

    public MonolithSystem()
    {
        UserVariables = Cache.UserVariables;
        SystemVariables = Cache.SystemVariables;
        RunningProcesses = Cache.RunningProcesses;
        Services = Cache.Services;
        InstalledApps = Cache.InstalledApps;
        InstalledHotfixes = Cache.InstalledHotfixes;
        ScheduledTasks = Cache.ScheduledTasks;
        PowerProfiles = Cache.PowerProfiles;
        MicroCodes = Cache.MicroCodes;
        RecentMinidumps = Cache.RecentMinidumps;
        StaticCoreCount = Cache.StaticCoreCount;
        ChoiceRegistryValues = Cache.ChoiceRegistryValues;
        UsernameSpecialCharacters = Cache.UsernameSpecialCharacters;
        OneDriveCommercialPathLength = Cache.OneDriveCommercialPathLength;
        OneDriveCommercialNameLength = Cache.OneDriveCommercialNameLength;
        BrowserExtensions = Cache.BrowserExtensions;
        DefaultBrowser = Cache.DefaultBrowser;
    }
}

[Serializable]
public class MonolithNetwork
{
    public List<Dictionary<string, object>> Adapters;
    public List<Dictionary<string, object>> Adapters2;
    public List<Dictionary<string, object>> Routes;
    public List<NetworkConnection> NetworkConnections;
    public string HostsFile;
    public string HostsFileHash;

    public MonolithNetwork()
    {
        Adapters = Cache.NetAdapters;
        Adapters2 = Cache.NetAdapters2;
        Routes = Cache.IPRoutes;
        NetworkConnections = Cache.NetworkConnections;
        HostsFile = Cache.HostsFile;
        HostsFileHash = Cache.HostsFileHash;
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