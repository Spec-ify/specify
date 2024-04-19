using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Eventing.Reader;
using System.Xml.Serialization;
using System.Xml;
using System.Runtime.CompilerServices;

namespace specify_client.data;

using static specify_client.DebugLog;
using static Utils;

using Win32Task = Microsoft.Win32.TaskScheduler.Task;
using TaskFolder = Microsoft.Win32.TaskScheduler.TaskFolder;


public static partial class Cache
{
    public static async Task MakeSystemData()
    {
        try
        {
            
            Region region = Region.System;
            await StartRegion(region);

            List<Task> systemTaskList = new()
            {
                DoTask(region, "GetEnvironmentVariables", GetEnvironmentVariables),
                DoTask(region, "GetSystemWMIInfo", GetSystemWMIInfo),
                DoTask(region, "CheckCommercialOneDrive", CheckCommercialOneDrive),
                DoTask(region, "GetInstalledApps", GetInstalledApps),
                DoTask(region, "GetScheduledTasks", GetScheduledTasks),
                DoTask(region, "GetStartupTasks", GetStartupTasks),
                DoTask(region, "RegistryCheck", RegistryCheck),
                DoTask(region, "GetMicrocodes", GetMicroCodes),
                DoTask(region, "GetStaticCoreCount", GetStaticCoreCount),
                DoTask(region, "GetBrowserExtensions", GetBrowserExtensions),
                DoTask(region, "GetMinidumps", GetMiniDumps),
                DoTask(region, "GetDefaultBrowser", GetDefaultBrowser),
                DoTask(region, "GetProcesses", GetProcesses)
            };

            // Check if username contains non-alphanumeric characters
            UsernameSpecialCharacters = !Regex.IsMatch(Environment.UserName, @"^[a-zA-Z0-9]+$");

            await Task.WhenAll(systemTaskList);
            await EndRegion(region);
        }
        catch (Exception ex)
        {
            await LogFatalError($"{ex}", Region.System);
        }
        SystemWriteSuccess = true;
    }
    private static async Task GetProcesses()
    {
        var outputProcesses = new List<OutputProcess>();
        var rawProcesses = Process.GetProcesses();

        foreach (var rawProcess in rawProcesses)
        {
            var cpuPercent = 1.0; // TODO: make this actually work properly
            string exePath;
            /*try
            {
                var counter = new PerformanceCounter("Process", "% Processor Time", rawProcess.ProcessName);
                counter.NextValue();
                Thread.Sleep(100);
                cpuPercent = counter.NextValue();
            }
            catch (Win32Exception e)
            {
                cpuPercent = -1;
            }*/

            try
            {
                // capacity must be declared so it can be referenced.
                var capacity = 2000;
                var sb = new StringBuilder(capacity);

                var ptr = Interop.OpenProcess(Interop.ProcessAccessFlags.QueryLimitedInformation, false, rawProcess.Id);

                if (!Interop.QueryFullProcessImageName(ptr, 0, sb, ref capacity))
                {
                    if (!SystemProcesses.Contains(rawProcess.ProcessName))
                    {
                        exePath = "Not Found";
                        await LogEventAsync($"System Data: Could not get the EXE path of {rawProcess.ProcessName} ({rawProcess.Id})", Region.System, EventType.WARNING);
                    }
                    else exePath = "SYSTEM";
                }
                else exePath = sb.ToString();
            }
            catch (Win32Exception e)
            {
                exePath = "null - See Debug Log.";
                await LogEventAsync($"System Data: Could not get the EXE path of {rawProcess.ProcessName} ({rawProcess.Id})", Region.System, EventType.ERROR);
                await LogEventAsync($"{e}", Region.System);
            }

            outputProcesses.Add(new OutputProcess
            {
                ProcessName = rawProcess.ProcessName,
                ExePath = exePath,
                Id = rawProcess.Id,
                WorkingSet = rawProcess.WorkingSet64,
                CpuPercent = cpuPercent
            });
        }
        RunningProcesses = outputProcesses;
    }

    private static void GetInstalledApps()
    {

        // Code Adapted from https://social.msdn.microsoft.com/Forums/en-US/94c2f14d-c45e-4b55-9ba0-eb091bac1035/c-get-installed-programs, thanks Rajasekhar.R! - K97i
        // Currently throws a hissy fit, NullReferenceException when actually adding to the Class

        List<InstalledApp> apps = new List<InstalledApp>();

        var hckuList = GetInstalledAppsAtKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", Registry.CurrentUser);
        var lm32List = GetInstalledAppsAtKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", Registry.LocalMachine);
        var lm64List = GetInstalledAppsAtKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", Registry.LocalMachine);
        apps.AddRange(hckuList);
        apps.AddRange(lm32List);
        apps.AddRange(lm64List);

        InstalledApps = apps;
    }

    private static List<InstalledApp> GetInstalledAppsAtKey(string keyLocation, RegistryKey reg)
    {
        var InstalledApps = new List<InstalledApp>();

        var key = reg.OpenSubKey(keyLocation);
        if (key is null)
        {
            LogEvent($"Registry Read Error @ {keyLocation}", Region.System, EventType.ERROR);
            return InstalledApps;
        }
        foreach (string keyName in key.GetSubKeyNames())
        {
            RegistryKey subkey = key.OpenSubKey(keyName);
            var appName = subkey.GetValue("DisplayName") as string;
            var appVersion = subkey.GetValue("DisplayVersion") as string;
            var appDate = subkey.GetValue("InstallDate") as string;

            if (appName == null)
            {
                //LogEvent($"null app name found @ {keyLocation}", Region.System, EventType.ERROR);
                continue;
            }

            if (appName != null)
            {
                InstalledApps.Add(
                    new InstalledApp()
                    {
                        Name = appName,
                        Version = appVersion,
                        InstallDate = appDate
                    });
            }
        }
        return InstalledApps;
    }

    public static async Task<StartupTask> CreateStartupTask(string appName, string imagePath)
    {
        StartupTask startupTask = new();
        startupTask.AppName = appName;
        if (string.IsNullOrEmpty(imagePath))
        {
            startupTask.ImagePath = "Image Path not found";
            await LogEventAsync($"No ImagePath data found for {appName}", Region.System, EventType.WARNING);
            return startupTask;
        }
        else
        {
            startupTask.ImagePath = imagePath;
        }
        startupTask = await GetFileInformation(startupTask);
        return startupTask;
    }

    private static List<Win32Task> EnumScheduledTasks(TaskFolder fld)
    {
        var res = fld.Tasks.ToList();
        foreach (var sfld in fld.SubFolders)
            res.AddRange(EnumScheduledTasks(sfld));

        return res;
    }

    // Returns startup tasks from the following locations:
    // Group 1: HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
    // Group 2: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
    // Group 3: HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run
    // Group 4: %AppData%\Microsoft\Windows\Start Menu\Programs\Startup
    public static async Task GetStartupTasks()
    {
        List<StartupTask> startupTasks = new();

        var group1TaskList = await GetStartupTasksAtKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Registry.CurrentUser);
        var group2TaskList = await GetStartupTasksAtKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Registry.LocalMachine);
        var group3TaskList = await GetStartupTasksAtKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", Registry.LocalMachine);
        var group4TaskList = await GetStartupTasksAtAppData();

        startupTasks.AddRange(group1TaskList);
        startupTasks.AddRange(group2TaskList);
        startupTasks.AddRange(group3TaskList);
        startupTasks.AddRange(group4TaskList);

        StartupTasks = startupTasks;
    }

    private static async Task<List<StartupTask>> GetStartupTasksAtKey(string keyLocation, RegistryKey reg)
    {
        List<StartupTask> startupTasks = new();
        var key = reg.OpenSubKey(keyLocation);
        if (key != null)
        {
            foreach (var appName in key.GetValueNames())
            {
                var startupTask = await CreateStartupTask(appName, (string)key.GetValue(appName));
                startupTasks.Add(startupTask);
            }
        }
        return startupTasks;
    }

    private static async Task<List<StartupTask>> GetStartupTasksAtAppData()
    {
        List<StartupTask> startupTasks = new();
        try
        {
            var startupFiles = Directory.GetFiles(Environment.ExpandEnvironmentVariables("%AppData%") + @"\Microsoft\Windows\Start Menu\Programs\Startup");
            foreach (var file in startupFiles)
            {
                string appName = Path.GetFileName(file);
                var startupTask = await CreateStartupTask(appName, file);
                startupTasks.Add(startupTask);
            }
        }
        catch (Exception ex)
        {
            await LogEventAsync($"File Read error in GetStartupTasksAtAppData", Region.System, EventType.ERROR);
            await LogEventAsync($"{ex}", Region.System);
        }
        return startupTasks;
    }

    public static async Task<StartupTask> StartupTaskFileError(StartupTask startupTask, Exception ex)
    {
        await LogEventAsync($"{startupTask.ImagePath} file not found for startup app {startupTask.AppName} - {ex}", Region.System, EventType.WARNING);
        startupTask.ImagePath += " - FILE NOT FOUND";
        return startupTask;
    }

    public static async Task<StartupTask> GetFileInformation(StartupTask startupTask)
    {
        //get information about an executable file
        var filePath = startupTask.ImagePath.Trim('\"');

        //trim shortcut target information
        var substringIndex = filePath.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);

        if (substringIndex != -1)
        {
            substringIndex += 4;
            filePath = filePath.Substring(0, substringIndex);
        }
        if (!File.Exists(filePath))
        {
            return await StartupTaskFileError(startupTask, new FileNotFoundException($"{filePath}"));
        }

        var timestamp = new FileInfo(filePath).LastWriteTime;
        startupTask.Timestamp = timestamp;
        var description = FileVersionInfo.GetVersionInfo(filePath).FileDescription;
        startupTask.AppDescription = description;
        return startupTask;
    }
    public static long DirectorySize(string[] filePaths)
    {
        long sum = 0;
        foreach(var path in filePaths)
        {
            FileInfo fileInfo = new(path);
            sum += fileInfo.Length;
        }
        return sum;
    }

    public static async Task GetMiniDumps()
    {
        string result = null;
        const string specifiedDumpDestination = "https://dumpload.spec-ify.com/";
        const string dumpDir = @"C:\Windows\Minidump";
        string TempFolder = Path.GetTempPath() + @"specify-dumps";
        string TempZip = Path.GetTempPath() + @"specify-dumps.zip";

        CountMinidumps();

        if (RecentMinidumps <= 0)
        {
            await LogEventAsync("No current dumps found.", Region.System);
            return;
        }

        string[] dumps = Directory.GetFiles(dumpDir);
        var directorySize = DirectorySize(dumps);
        if (directorySize > 0x10000000) // 256 MB
        {
            await LogEventAsync($"Dump files are too large to upload: {directorySize / 1024 / 1024} MB", Region.System, EventType.WARNING);
            return;
        }

        if (dumps.Length == 0)
        {
            await LogEventAsync($"Dumps could not be retrieved from {dumpDir}", Region.System, EventType.ERROR);
            return;
        }

        await LogEventAsync("Dump Upload Requested.", Region.System);
        if (MessageBox.Show("Would you like to upload your BSOD minidumps with your specs report?", "Minidumps detected", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
        {
            await LogEventAsync("Dump Upload Request Refused.", Region.System);
            return;
        }

        await LogEventAsync("Dump Upload Request Approved.", Region.System);

        Directory.CreateDirectory(TempFolder);

        if (!await CreateMinidumpZipFile(dumps, TempFolder, TempZip))
        {
            await LogEventAsync("Dump zip file creation failure.", Region.System, EventType.ERROR);
            return;
        }

        FileInfo fileInfo = new(TempZip);

        if(fileInfo.Length >= 0x8000000) // 128 MiB
        {
            await LogEventAsync($"Dump zip too large: {fileInfo.Length / 1024 / 1024} MB", Region.System, EventType.WARNING);
            File.Delete(TempZip);
            return;
        }

        await LogEventAsync($"Dump zip file built. Attempting upload. - Size: {fileInfo.Length:X}", Region.System);

        result = await UploadMinidumps(TempZip, specifiedDumpDestination);
        if (string.IsNullOrEmpty(result))
        {
            await LogEventAsync($"Dump upload failure. {result}", Region.System, EventType.ERROR);
            return;
        }

        await LogEventAsync($"Dump file upload result: {result}", Region.System);
        File.Delete(TempZip);
        new DirectoryInfo(TempFolder).Delete(true);

        DumpZip = result;
    }
    private static async Task<bool> CreateMinidumpZipFile(string[] dumps, string TempFolder, string TempZip)
    {
        try
        {
            bool copied = false;
            foreach (string dump in dumps)
            {
                // Skip any non-dump files, if any.
                if (!dump.ToLower().Contains(".dmp")) 
                {
                    await LogEventAsync($"{dump} is not a dump file.", Region.System, EventType.WARNING);
                    continue;
                }

                var fileName = string.Concat(TempFolder + @"/", Regex.Match(dump, "[^\\\\]*$").Value);
                File.Copy(dump, fileName);
                if (!File.Exists(fileName))
                {
                    await LogEventAsync($"Failed to copy {Regex.Match(dump, "[^\\\\]*$").Value} to dump folder.", Region.System, EventType.ERROR);
                }
                else
                {
                    copied = true;
                }
            }
            // If at least one dump was successfully copied, create the directory and return success.
            if (copied)
            {
                ZipFile.CreateFromDirectory(TempFolder, TempZip);
                return true;
            }
            else return false;
        }
        catch (Exception e)
        {
            await LogEventAsync($"Error occured manipulating dump files! Is this running as admin?", Region.System, EventType.ERROR);
            await LogEventAsync($"{e}", Region.System);

            return false; //If this failed, there's nothing more that can be done here.
        }
    }

    private static async Task<string> UploadMinidumps(string TempZip, string specifiedDumpDestination)
    {
        string result = string.Empty;
        using (HttpClient client = new HttpClient())
        using (MultipartFormDataContent form = new MultipartFormDataContent())
        {
            FileStream dumpStream = new FileStream(TempZip, FileMode.Open);

            form.Add(new StreamContent(dumpStream), "file", "file.zip");

            try
            {
                HttpResponseMessage response = await client.PostAsync(specifiedDumpDestination, form);

                string rawlink = response.Content.ReadAsStringAsync().Result;

                result = Regex.Replace(rawlink, @"\t|\n|\r", "");
            }
            catch (Exception e)
            {
                await LogEventAsync($"Error occured when uploading dumps.zip to Specified!", Region.System, EventType.ERROR);
                await LogEventAsync($"{e}", Region.System);
            }

            client.Dispose();
        }
        return result;
    }

    private static void GetMicroCodes()
    {
        const string intelPath = @"C:\Windows\System32\mcupdate_genuineintel.dll";
        const string amdPath = @"C:\Windows\System32\mcupdate_authenticamd.dll";

        var res = new List<string>();
        if (File.Exists(intelPath)) res.Add(intelPath);
        if (File.Exists(amdPath)) res.Add(amdPath);

        MicroCodes = res;
    }

    private static async Task GetStaticCoreCount()
    {
        string output = string.Empty;

        var procStartInfo = new ProcessStartInfo("bcdedit", "/enum")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var proc = new Process())
        {
            proc.StartInfo = procStartInfo;
            proc.Start();
            output = proc.StandardOutput.ReadToEnd();
            if (output.Contains("The boot configuration data store could not be opened"))
            {
                await LogEventAsync("Could not check whether there is a static core count", Region.System, EventType.ERROR);
                StaticCoreCount = null;
            }
            StaticCoreCount = output.Contains("numproc");
        }
    }

    private static void CountMinidumps()
    {
        const string dumpPath = @"C:\Windows\Minidump";
        var count = 0;

        if (!Directory.Exists(dumpPath)) return;

        var files = Directory.GetFiles(dumpPath);

        foreach (var file in files)
        {
            var lastWriteTime = File.GetLastWriteTime(file);

            if (lastWriteTime > DateTime.Now.AddMonths(-1))
            {
                count++;
            }
        }
        RecentMinidumps = count;
    }

    private static async Task RegistryCheck()
    {
        try
        {

            LastBiosTime = new RegistryValue<int?>
                (Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "FwPOSTTime").Value;
            var tdrLevel = new RegistryValue<int?>
                (Registry.LocalMachine, @"System\CurrentControlSet\Control\GraphicsDrivers", "TdrLevel");
            var nbFLimit = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Psched", "NonBestEffortLimit");
            var throttlingIndex = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex");
            var superFetch = new RegistryValue<int?>
                (Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch");

            // Defender 1
            var disableAv = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender", "DisableAntiVirus");
            var disableAs = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender", "DisableAntiSpyware");
            var passiveMode = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender", "PassiveMode");
            passiveMode.Name = "Windows Defender\\Passive Mode"; // This edit makes it easier to understand what the registry entry is.
            var puaProtection = new RegistryValue<int?>
                (Registry.LocalMachine, @"\SOFTWARE\Microsoft\Windows Defender", "PUAProtection");
            // Defender 2
            var disableAvpolicy = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiVirus");
            var disableAspolicy = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware");
            var passiveModepolicy = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "PassiveMode");
            passiveModepolicy.Name = "Windows Defender\\Passive Mode";
            var puaProtectionpolicy = new RegistryValue<int?>
                (Registry.LocalMachine, @"\SOFTWARE\Policies\Microsoft\Windows Defender", "PUAProtection");

            var drii = new RegistryValue<int?>
                (Registry.LocalMachine, @"\Software\Policies\Microsoft\MRT", "DontReportInfectionInformation");
            var disableWer = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled");
            disableWer.Name = "Windows Error Reporting\\Disabled";
            var unsupportedTpmOrCpu = new RegistryValue<int?>
                (Registry.LocalMachine, @"SYSTEM\Setup\MoSetup", "AllowUpgradesWithUnsupportedTPMOrCPU");
            var hwSchMode = new RegistryValue<int?>
                (Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
            var WUServer = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "UseWUServer");
            var noAutoUpdate = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate");
            var fastBoot = new RegistryValue<int?>
                (Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled");
            var auditBoot = new RegistryValue<int?>
                (Registry.LocalMachine, @"SYSTEM\Setup\Status\", "AuditBoot");
            var previewBuilds = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds\", "AllowBuildPreview");
            var bypassCpuCheck = new RegistryValue<int?>(Registry.LocalMachine, @"SYSTEM\Setup\LabConfig", "BypassCPUCheck");
            var bypassStorageCheck = new RegistryValue<int?>(Registry.LocalMachine, @"SYSTEM\Setup\LabConfig", "BypassStorageCheck");
            var bypassTpmCheck = new RegistryValue<int?>(Registry.LocalMachine, @"SYSTEM\Setup\LabConfig", "BypassTPMCheck");
            var bypassRamCheck = new RegistryValue<int?>(Registry.LocalMachine, @"SYSTEM\Setup\LabConfig", "BypassRAMCheck");
            var bypassSecureBootCheck = new RegistryValue<int?>(Registry.LocalMachine, @"SYSTEM\Setup\LabConfig", "BypassSecureBootCheck");
            var hwNotificationCache = new RegistryValue<int?>(Registry.CurrentUser, @"Control Panel\UnsupportedHardwareNotificationCache", "SV2");
            hwNotificationCache.Name = "UnsupportedHardwareNotificationCache\\SV2";
            var prioritySeparation = new RegistryValue<int?>(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation");

            ChoiceRegistryValues = new List<IRegistryValue>()
            {
                tdrLevel, nbFLimit, throttlingIndex, superFetch, disableAv, disableAs, puaProtection, passiveMode, disableAvpolicy, disableAspolicy,
                puaProtectionpolicy, passiveModepolicy, drii, disableWer,unsupportedTpmOrCpu, hwSchMode, WUServer, noAutoUpdate, fastBoot, auditBoot,
                previewBuilds, bypassCpuCheck, bypassStorageCheck, bypassRamCheck, bypassTpmCheck, bypassSecureBootCheck, hwNotificationCache, prioritySeparation
            };
        }
        catch (Exception ex)
        {
            await LogEventAsync("Registry Read Error in RegistryCheck()", Region.System, EventType.ERROR);
            await LogEventAsync($"{ex}");
            ChoiceRegistryValues = new List<IRegistryValue>();
        }
    }

    private static async Task GetBrowserExtensions()
    {
        List<Browser> Browsers = new List<Browser>();
        string UserPath = string.Concat("C:\\Users\\", Username, "\\Appdata\\");
        Dictionary<string, string> BrowserPaths = new Dictionary<string, string>()
        {
            {"Edge", "Local\\Microsoft\\Edge\\User Data\\"},
            {"Vivaldi", "Local\\Vivaldi\\User Data\\"},
            {"Brave", "Local\\BraveSoftware\\Brave-Browser\\User Data\\"},
            {"Chrome", "Local\\Google\\Chrome\\User Data\\"},
            {"Firefox", "Roaming\\Mozilla\\Firefox\\Profiles\\"},
            {"OperaGX","Roaming\\Opera Software\\Opera GX Stable\\"}
        };

        foreach (KeyValuePair<string, string> BrowserPath in BrowserPaths)
        {
            if (Directory.Exists(string.Concat(UserPath, BrowserPath.Value)))
            {
                if (BrowserPath.Key.Equals("Firefox"))
                {
                    Browser browser = new Browser()
                    {
                        Name = "Firefox",
                        Profiles = new List<Browser.BrowserProfile>()
                    };

                    foreach (string dir in Directory.GetDirectories(string.Concat(UserPath, BrowserPath.Value)))
                    {
                        try
                        {
                            var addonsFile = string.Concat(dir, "\\addons.json");
                            if (!File.Exists(addonsFile)) continue;
                            List<JToken> extensions = JObject.Parse(File.ReadAllText(addonsFile))["addons"].Children().ToList();
                            Browser.BrowserProfile profile = new Browser.BrowserProfile()
                            {
                                name = new DirectoryInfo(dir).Name.Substring(8),
                                Extensions = new List<Browser.Extension>()
                            };

                            foreach (JToken extension in extensions)
                                profile.Extensions.Add(new Browser.Extension()
                                {
                                    name = (string)extension["name"],
                                    description = (string)extension["description"],
                                    version = (string)extension["version"]
                                });

                            browser.Profiles.Add(profile);
                        }
                        catch (Exception e)
                        {
                            await LogEventAsync($"Exception occurred in GetBrowserExtensions() during browser enumeration.", Region.System, EventType.ERROR);
                            await LogEventAsync($"{e}");
                        }
                    }

                    Browsers.Add(browser);
                }
                else if (BrowserPath.Key.Equals("OperaGX"))
                {
                    Browser browser = new Browser()
                    {
                        Name = "OperaGX",
                        Profiles = new List<Browser.BrowserProfile>()
                    };
                    List<string> defaultExtensions = new List<string>() //Extensions installed by default, we can ignore these
                    {
                        "aelmefcddnelhophneodelaokjogeemi",
                        "enegjkbbakeegngfapepobipndnebkdk",
                        "gojhcdgcpbpfigcaejpfhfegekdgiblk",
                        "kbmoiomgmchbpihhdpabemajcbjpcijk"
                    };
                    Browser.BrowserProfile profile = new Browser.BrowserProfile()
                    {
                        name = "Default",
                        Extensions = new List<Browser.Extension>()
                    };
                    //make sre the dirs actually exist
                    if (Directory.Exists(string.Concat(UserPath, BrowserPath.Value, "Extensions")))
                    {
                        //Default profile logic needs to exist seperately due to OperaGX's AppData file structure.
                        foreach (string edir in Directory.GetDirectories(string.Concat(UserPath, BrowserPath.Value, "Extensions")))
                        {
                            if (!defaultExtensions.Contains(new DirectoryInfo(edir).Name))
                            {
                                if (new DirectoryInfo(edir).Name.Equals("Temp"))
                                    continue;

                                try
                                {
                                    profile.Extensions.Add(ParseChromiumExtension(edir));
                                }
                                catch (Exception e)
                                {
                                    if (e is FileNotFoundException || e is JsonException)
                                        await LogEventAsync(string.Concat("Malformed or missing manifest or locale data for extension at ", edir), Region.System, EventType.WARNING);
                                    //DirectoryNotFoundException can occur with certain browsers when a profile exists but no extensions are installed
                                }
                            }
                        }
                    }
                    browser.Profiles.Add(profile);

                    var sideProfilesPath = string.Concat(UserPath, BrowserPath.Value, "_side_profiles");
                    if (Directory.Exists(sideProfilesPath))
                    {
                        //Fetch side profiles
                        foreach (string pdir in Directory.GetDirectories(sideProfilesPath))
                        {
                            profile = JsonConvert.DeserializeObject<Browser.BrowserProfile>(
                                File.ReadAllText(Directory.GetFiles(pdir, "*sideprofile.json")[0]));
                            profile.Extensions = new List<Browser.Extension>();

                            foreach (string edir in Directory.GetDirectories(string.Concat(pdir, "\\Extensions")))
                            {
                                if (!defaultExtensions.Contains(new DirectoryInfo(edir).Name))
                                {
                                    if (new DirectoryInfo(edir).Name.Equals("Temp"))
                                        continue;

                                    try
                                    {
                                        profile.Extensions.Add(ParseChromiumExtension(edir));
                                    }
                                    catch (Exception e)
                                    {
                                        if (e is FileNotFoundException || e is JsonException)
                                            await LogEventAsync(string.Concat("Malformed or missing manifest or locale data for extension at ", edir), Region.System, EventType.WARNING);
                                    }
                                }
                            }

                            browser.Profiles.Add(profile);
                        }
                    }

                    Browsers.Add(browser);
                }
                else //Chromium Browsers
                {
                    Browser browser = new Browser()
                    {
                        Name = BrowserPath.Key,
                        Profiles = new List<Browser.BrowserProfile>()
                    };
                    List<string> directories = Directory.GetDirectories(string.Concat(UserPath, BrowserPath.Value), "Profile*").ToList();
                    directories.Add(string.Concat(UserPath, BrowserPath.Value, "Default"));

                    foreach (string dir in directories)
                    {
                        Browser.BrowserProfile profile = new Browser.BrowserProfile()
                        {
                            name = new DirectoryInfo(dir).Name,
                            Extensions = new List<Browser.Extension>()
                        };
                        try
                        {
                            foreach (string edir in Directory.GetDirectories(string.Concat(dir, "\\Extensions")))
                            {
                                if (new DirectoryInfo(edir).Name.Equals("Temp"))
                                    continue;

                                try
                                {
                                    profile.Extensions.Add(ParseChromiumExtension(edir));
                                }
                                catch (Exception e)
                                {
                                    if (e is FileNotFoundException || e is JsonException)
                                        await LogEventAsync(string.Concat("Malformed or missing manifest or locale data for extension at ", edir), Region.System, EventType.WARNING);
                                    //DirectoryNotFoundException can occur with certain browsers when a profile exists but no extensions are installed
                                }
                            }
                        }
                        catch (DirectoryNotFoundException)
                        {
                            //Do nothing, this means no extensions are installed
                        }

                        browser.Profiles.Add(profile);
                    }

                    Browsers.Add(browser);
                }
            }
        }
        BrowserExtensions = Browsers;
    }

    private static async Task CheckCommercialOneDrive()
    {
        bool ODFound = false;
        try
        {
            var uv = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            if (uv["OneDriveCommercial"] is string pathOneDriveCommercial)
            {
                var actualOneDriveCommercial =
                    pathOneDriveCommercial.Split(new string[] { "OneDrive - " }, StringSplitOptions.None)[1];
                OneDriveCommercialPathLength = pathOneDriveCommercial.Length;
                OneDriveCommercialNameLength = actualOneDriveCommercial.Length;
                await LogEventAsync("OneDriveCommercial information retrieved.", Region.System);
                ODFound = true;
            }
        }
        finally
        {
            if (!ODFound)
            {
                if (Settings.RedactOneDriveCommercial)
                {
                    Settings.RedactOneDriveCommercial = false;
                    await LogEventAsync("RedactOneDriveCommercial setting disabled. OneDriveCommercial variable not found.", Region.System, EventType.WARNING);
                }
                else
                {
                    await LogEventAsync("OneDriveCommercial variable not found.", Region.System);
                }
            }
        }
    }

    private static void GetScheduledTasks()
    {
        var scheduledTasks = new List<ScheduledTask>();
        using var ts = new Microsoft.Win32.TaskScheduler.TaskService();
        var rawTaskList = EnumScheduledTasks(ts.RootFolder);
        WinScheduledTasks = new List<ScheduledTask>();
        foreach (Win32Task task in rawTaskList)
        {
            if (task.Path.StartsWith("\\Microsoft"))
            {
                WinScheduledTasks.Add(new ScheduledTask(task));
            }
            else
            {
                scheduledTasks.Add(new ScheduledTask(task));
            }
        }
        ScheduledTasks = scheduledTasks;

    }

    private static async Task GetDefaultBrowser()
    {
        try
        {
            string defaultBrowserProcess = Regex.Match(GetRegistryValue<string>(Registry.ClassesRoot, string.Concat(GetRegistryValue<string>(Registry.CurrentUser,
                "Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\https\\UserChoice", "ProgID"), "\\shell\\open\\command"), ""), "\\w*.exe").Value;
            DefaultBrowser = (defaultBrowserProcess.Equals("Launcher.exe")) ? "OperaGX" : defaultBrowserProcess;
        }
        catch (Exception e)
        {
            await LogEventAsync("Could not detect default browser", Region.System, EventType.ERROR);
            await LogEventAsync($"{e}", Region.System);
        }
    }

    private static List<Dictionary<string, object>> GetPowerProfiles()
    {
        try
        {
            return GetWmi("Win32_PowerPlan", "*", @"root\cimv2\power");
        }
        catch (COMException)
        {
            LogEvent("Could not get power profiles", Region.System, EventType.ERROR);
            return new();
        }
    }

    private static void GetEnvironmentVariables()
    {
        SystemVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
        UserVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
    }

    private static void GetSystemWMIInfo()
    {
        Services = GetWmi("Win32_Service", "Name, Caption, PathName, StartMode, State");
        InstalledHotfixes = GetWmi("Win32_QuickFixEngineering", "Description,HotFixID,InstalledOn");
        // As far as I can tell, Size is the size of the file on the filesystem and Usage is the amount actually used
        PageFile = GetWmi("Win32_PageFileUsage", "AllocatedBaseSize, Caption, CurrentUsage, PeakUsage").FirstOrDefault();

        PowerProfiles = GetPowerProfiles();
    }
}