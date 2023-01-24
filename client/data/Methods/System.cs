using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace specify_client.data;

public static partial class Cache
{
    public static async System.Threading.Tasks.Task MakeSystemData()
    {
        try
        {
            List<System.Threading.Tasks.Task> DebugTasks = new();
            DebugLog.Region region = DebugLog.Region.System;
            await DebugLog.StartRegion(region);

            SystemVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
            UserVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            DebugTasks.Add(DebugLog.LogEventAsync("Environment Variables retrieved.", region));

            bool ODFound = false;
            try
            {
                if (UserVariables["OneDriveCommercial"] is string pathOneDriveCommercial)
                {
                    var actualOneDriveCommercial =
                        pathOneDriveCommercial.Split(new string[] { "OneDrive - " }, StringSplitOptions.None)[1];
                    OneDriveCommercialPathLength = pathOneDriveCommercial.Length;
                    OneDriveCommercialNameLength = actualOneDriveCommercial.Length;
                    DebugTasks.Add(DebugLog.LogEventAsync("OneDriveCommercial information retrieved.", region));
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
                        DebugTasks.Add(DebugLog.LogEventAsync("RedactOneDriveCommercial setting disabled. OneDriveCommercial variable not found.", region, DebugLog.EventType.WARNING));
                    }
                    else
                    {
                        DebugTasks.Add(DebugLog.LogEventAsync("OneDriveCommercial variable not found.", region));
                    }
                }
            }

            Services = Utils.GetWmi("Win32_Service", "Name, Caption, PathName, StartMode, State");
            InstalledHotfixes = Utils.GetWmi("Win32_QuickFixEngineering", "Description,HotFixID,InstalledOn");
            // As far as I can tell, Size is the size of the file on the filesystem and Usage is the amount actually used
            PageFile = Utils.GetWmi("Win32_PageFileUsage", "AllocatedBaseSize, Caption, CurrentUsage, PeakUsage").First();
            try
            {
                PowerProfiles = Utils.GetWmi("Win32_PowerPlan", "*", @"root\cimv2\power");
            }
            catch (COMException)
            {
                Issues.Add("Could not get power profiles");
            }
            DebugTasks.Add(DebugLog.LogEventAsync("System WMI Information retrieved.", region));

            InstalledApps = GetInstalledApps();
            DebugTasks.Add(DebugLog.LogEventAsync("InstalledApps Information retrieved.", region));

            var ts = new TaskService();
            var rawTaskList = EnumScheduledTasks(ts.RootFolder);
            ScheduledTasks = new List<ScheduledTask>();
            WinScheduledTasks = new List<ScheduledTask>();
            foreach (Task task in rawTaskList)
                if (task.Path.StartsWith("\\Microsoft"))
                    WinScheduledTasks.Add(new ScheduledTask(task));
                else
                    ScheduledTasks.Add(new ScheduledTask(task));
            DebugTasks.Add(DebugLog.LogEventAsync("ScheduledTasks Information retrieved.", region));

            StartupTasks = await GetStartupTasks();
            DebugTasks.Add(DebugLog.LogEventAsync("StartupTasks Information retrieved.", region));

            ChoiceRegistryValues = RegistryCheck();
            DebugTasks.Add(DebugLog.LogEventAsync("ChoiceRegistryValues Information retrieved.", region));

            MicroCodes = GetMicroCodes();
            DebugTasks.Add(DebugLog.LogEventAsync("MicroCodes Information retrieved.", region));

            RecentMinidumps = CountMinidumps();
            DebugTasks.Add(DebugLog.LogEventAsync("Minidumps counted.", region));

            StaticCoreCount = GetStaticCoreCount();
            DebugTasks.Add(DebugLog.LogEventAsync("StaticCoreCount retrieved.", region));

            BrowserExtensions = GetBrowserExtensions();
            DebugTasks.Add(DebugLog.LogEventAsync("Browser Extension Information retrieved.", region));

            string defaultBrowserProgID = Utils.GetRegistryValue<string>(Registry.CurrentUser, "Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\https\\UserChoice", "ProgID");
            string defaultBrowserProcess = Regex.Match(Utils.GetRegistryValue<string>(Registry.ClassesRoot, string.Concat(Utils.GetRegistryValue<string>(Registry.CurrentUser,
                "Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\https\\UserChoice", "ProgID"), "\\shell\\open\\command"), ""), "\\w*.exe").Value;
            DefaultBrowser = (defaultBrowserProcess.Equals("Launcher.exe")) ? "OperaGX" : defaultBrowserProcess;
            DebugTasks.Add(DebugLog.LogEventAsync("Default Browser Infomation retrieved.", region));

            RunningProcesses = GetProcesses();
            DebugTasks.Add(DebugLog.LogEventAsync("RunningProcess Information retrieved.", region));

            // Check if username contains non-alphanumeric characters
            UsernameSpecialCharacters = !Regex.IsMatch(Environment.UserName, @"^[a-zA-Z0-9]+$");
            await System.Threading.Tasks.Task.WhenAll(DebugTasks);
            await DebugLog.EndRegion(DebugLog.Region.System);
        }
        catch (Exception ex)
        {
            await DebugLog.LogEventAsync("UNEXPECTED FATAL EXCEPTION", DebugLog.Region.System, DebugLog.EventType.ERROR);
            await DebugLog.LogEventAsync($"{ex}", DebugLog.Region.System);
            Environment.Exit(-1);
        }
    }

    private static List<OutputProcess> GetProcesses()
    {
        DateTime start = DateTime.Now;
        DebugLog.LogEvent("GetProcesses() started", DebugLog.Region.System);
        List<OutputProcess> outputProcesses = new List<OutputProcess>();
        var rawProcesses = Process.GetProcesses();

        foreach (var rawProcess in rawProcesses)
        {
            var cpuPercent = 1.0; // TODO: make this actually work properly
            var exePath = "";
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
                        Issues.Add($"System Data: Could not get the EXE path of {rawProcess.ProcessName} ({rawProcess.Id})");
                    }
                    else
                    {
                        exePath = "SYSTEM";
                    }
                }
                else
                {
                    exePath = sb.ToString();
                }
            }
            catch (Win32Exception e)
            {
                exePath = "null - Win32Exception";
                Issues.Add($"System Data: Could not get the EXE path of {rawProcess.ProcessName} ({rawProcess.Id})");
                Console.WriteLine(e.GetBaseException());
            }
            bool processExists = false;
            foreach(var outputProcess in outputProcesses)
            {
                if(outputProcess.ExePath.ToLower().Equals(exePath.ToLower()) && exePath != "SYSTEM")
                {
                    outputProcess.WorkingSet += rawProcess.WorkingSet64;
                    outputProcess.CpuPercent += cpuPercent;
                    processExists = true;
                    break;
                }
            }
            if (!processExists)
            {
                outputProcesses.Add(new OutputProcess
                {
                    ProcessName = rawProcess.ProcessName,
                    ExePath = exePath,
                    Id = rawProcess.Id,
                    WorkingSet = rawProcess.WorkingSet64,
                    CpuPercent = cpuPercent
                });
            }
        }
        DebugLog.LogEvent($"GetProcesses() completed. Total runtime: {(DateTime.Now - start).TotalMilliseconds}", DebugLog.Region.System);
        return outputProcesses;
    }

    private static List<InstalledApp> GetInstalledApps()
    {
        // Code Adapted from https://social.msdn.microsoft.com/Forums/en-US/94c2f14d-c45e-4b55-9ba0-eb091bac1035/c-get-installed-programs, thanks Rajasekhar.R! - K97i
        // Currently throws a hissy fit, NullReferenceException when actually adding to the Class

        List<InstalledApp> InstalledApps = new List<InstalledApp>();

        string appName, appVersion, appDate;
        RegistryKey key;

        // Current User
        key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        foreach (String keyName in key.GetSubKeyNames())
        {
            RegistryKey subkey = key.OpenSubKey(keyName);
            appName = subkey.GetValue("DisplayName") as string;
            appVersion = subkey.GetValue("DisplayVersion") as string;
            appDate = subkey.GetValue("InstallDate") as string;

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

        // Local Machine 32
        key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        foreach (String keyName in key.GetSubKeyNames())
        {
            RegistryKey subkey = key.OpenSubKey(keyName);
            appName = subkey.GetValue("DisplayName") as string;
            appVersion = subkey.GetValue("DisplayVersion") as string;
            appDate = subkey.GetValue("InstallDate") as string;

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

        // Local Machine 64
        key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        foreach (String keyName in key.GetSubKeyNames())
        {
            RegistryKey subkey = key.OpenSubKey(keyName);
            appName = subkey.GetValue("DisplayName") as string;
            appVersion = subkey.GetValue("DisplayVersion") as string;
            appDate = subkey.GetValue("InstallDate") as string;

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

    public static async System.Threading.Tasks.Task<StartupTask> CreateStartupTask(string appName, string? imagePath)
    {
        StartupTask startupTask = new();
        startupTask.AppName = appName;
        if (string.IsNullOrEmpty(imagePath))
        {
            startupTask.ImagePath = "Image Path not found";
            Issues.Add($"Data not found for Startup app {appName}");
            await DebugLog.LogEventAsync($"No ImagePath data found for {appName}", DebugLog.Region.System, DebugLog.EventType.WARNING);
            return startupTask;
        }
        else
        {
            startupTask.ImagePath = imagePath;
        }
        startupTask = await GetFileInformation(startupTask);
        return startupTask;
    }

    private static List<Task> EnumScheduledTasks(TaskFolder fld)
    {
        var res = fld.Tasks.ToList();
        foreach (var sfld in fld.SubFolders)
            res.AddRange(EnumScheduledTasks(sfld));

        return res;
    }
    // Returns startup tasks from the following locations:
    // 1: HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
    // 2: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
    // 3: HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run
    // 4: %AppData%\Microsoft\Windows\Start Menu\Programs\Startup
    public static async System.Threading.Tasks.Task<List<StartupTask>> GetStartupTasks()
    {
        DateTime start = DateTime.Now;
        await DebugLog.LogEventAsync("GetStartupTasks() Started", DebugLog.Region.System);
        List<StartupTask> startupTasks = new();

        //
        // Group 1
        //

        // The intent of WorkingSource is to label each startup task by where it is on the PC. It's not implemented, and I'm not sure it's necessary.
        string WorkingSource = @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
        foreach (var appName in key.GetValueNames())
        {
            var startupTask = await CreateStartupTask(appName, (string)key.GetValue(appName));
            startupTasks.Add(startupTask);
        }

        //
        // Group 2
        //
        WorkingSource = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
        foreach (var appName in key.GetValueNames())
        {
            var startupTask = await CreateStartupTask(appName, (string)key.GetValue(appName));
            startupTasks.Add(startupTask);
        }

        //
        // Group 3
        //
        WorkingSource = @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
        key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run");
        foreach (var appName in key.GetValueNames())
        {
            var startupTask = await CreateStartupTask(appName, (string)key.GetValue(appName));
            startupTasks.Add(startupTask);
        }

        //
        // Group 4 - This should be expanded to get file information of the shortcuts' target application.
        //
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
            await DebugLog.LogEventAsync($"File Read error in group 4 of GetStartupTasks", DebugLog.Region.System, DebugLog.EventType.ERROR);
            await DebugLog.LogEventAsync($"{ex}", DebugLog.Region.System);
        }
        await DebugLog.LogEventAsync($"GetStartupTasks() Completed. Total Runtime: {(DateTime.Now - start).TotalMilliseconds}", DebugLog.Region.System);
        return startupTasks;
    }

    public static async System.Threading.Tasks.Task<StartupTask> StartupTaskFileError(StartupTask startupTask, Exception ex)
    {
        Issues.Add($"{startupTask.ImagePath} file not found for startup app {startupTask.AppName}");
        await DebugLog.LogEventAsync($"{startupTask.ImagePath} file not found for startup app {startupTask.AppName} - {ex}", DebugLog.Region.System, DebugLog.EventType.WARNING);
        startupTask.ImagePath += " - FILE NOT FOUND";
        return startupTask;
    }

    public static async System.Threading.Tasks.Task<StartupTask> GetFileInformation(StartupTask startupTask)
    {
        //get information about an executable file
        // .Trim requires a character array.
        char[] charArray = new char[1];
        charArray[0] = '\"';
        var filePath = startupTask.ImagePath.Trim(charArray);

        // Trim the target path to be a locateable filepath.
        var substringIndex = filePath.ToLower().IndexOf(".exe\"");
        if (substringIndex == -1)
        {
            // If the target path is not wrapped in quotes, look for a space after the filename instead.
            substringIndex = filePath.IndexOf(".exe ");
        }

        // If there is neither a space or quote after .exe, we shouldn't need to substring.
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

    private static List<string> GetMicroCodes()
    {
        const string intelPath = @"C:\Windows\System32\mcupdate_genuineintel.dll";
        const string amdPath = @"C:\Windows\System32\mcupdate_authenticamd.dll";

        var res = new List<string>();
        if (File.Exists(intelPath)) res.Add(intelPath);
        if (File.Exists(amdPath)) res.Add(amdPath);

        return res;
    }
    private static bool? GetStaticCoreCount()
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
                Issues.Add("Could not check whether there is a static core count");
                return null;
            }
            return output.Contains("numproc");
        }
    }
    private static int CountMinidumps()
    {
        const string dumpPath = @"C:\Windows\Minidump";
        var count = 0;

        if (!Directory.Exists(dumpPath)) return 0;

        var files = Directory.GetFiles(dumpPath);

        foreach (var file in files)
        {
            var lastWriteTime = File.GetLastWriteTime(file);

            if (lastWriteTime > DateTime.Now.AddDays(-7))
            {
                count++;
            }
        }
        return count;
    }
    private static List<IRegistryValue> RegistryCheck()
    {
        try
        {
            var tdrLevel = new RegistryValue<int?>
                (Registry.LocalMachine, @"System\CurrentControlSet\Control\GraphicsDrivers", "TdrLevel");
            var nbFLimit = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Psched", "NonBestEffortLimit");
            var throttlingIndex = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex");
            var superFetch = new RegistryValue<int?>
                (Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch");
            var disableAv = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiVirus");
            var disableAs = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware");
            var puaProtection = new RegistryValue<int?>
                (Registry.LocalMachine, @"\SOFTWARE\Policies\Microsoft\Windows Defender", "PUAProtection");
            var drii = new RegistryValue<int?>
                (Registry.LocalMachine, @"\Software\Policies\Microsoft\MRT", "DontReportInfectionInformation");
            var disableWer = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled");
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

            return new List<IRegistryValue>()
            {
                tdrLevel, nbFLimit, throttlingIndex, superFetch, disableAv, disableAs, puaProtection, drii, disableWer,
                unsupportedTpmOrCpu, hwSchMode, WUServer, noAutoUpdate, fastBoot, auditBoot, previewBuilds, bypassCpuCheck,
                bypassStorageCheck, bypassRamCheck, bypassTpmCheck, bypassSecureBootCheck, hwNotificationCache
            };
        }
        catch (Exception ex)
        {
            DebugLog.LogEvent("Registry Read Error in RegistryCheck()", DebugLog.Region.System, DebugLog.EventType.ERROR);
            DebugLog.LogEvent($"{ex}");
            return new List<IRegistryValue>();
        }

    }


    private static List<Browser> GetBrowserExtensions()
    {
        DateTime start = DateTime.Now;
        DebugLog.LogEvent("GetBrowserExtensions() started.", DebugLog.Region.System);
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
                            DebugLog.LogEvent($"Exception occurred in GetBrowserExtensions() during browser enumeration.", DebugLog.Region.System, DebugLog.EventType.ERROR);
                            DebugLog.LogEvent($"{e}");
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

                    //Default profile logic needs to exist seperately due to OperaGX's AppData file structure.
                    foreach (string edir in Directory.GetDirectories(string.Concat(UserPath, BrowserPath.Value, "Extensions")))
                    {
                        if (!defaultExtensions.Contains(new DirectoryInfo(edir).Name))
                        {
                            if (new DirectoryInfo(edir).Name.Equals("Temp"))
                                continue;

                            try
                            {
                                profile.Extensions.Add(Utils.ParseChromiumExtension(edir));
                            }
                            catch (Exception e)
                            {
                                if (e is FileNotFoundException || e is JsonException)
                                    Issues.Add(string.Concat("Malformed or missing manifest or locale data for extension at ", edir));
                                //DirectoryNotFoundException can occur with certain browsers when a profile exists but no extensions are installed
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
                                        profile.Extensions.Add(Utils.ParseChromiumExtension(edir));
                                    }
                                    catch (Exception e)
                                    {
                                        if (e is FileNotFoundException || e is JsonException)
                                            Issues.Add(string.Concat("Malformed or missing manifest or locale data for extension at ", edir));
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
                                    profile.Extensions.Add(Utils.ParseChromiumExtension(edir));
                                }
                                catch (Exception e)
                                {
                                    if (e is FileNotFoundException || e is JsonException)
                                        Issues.Add(string.Concat("Malformed or missing manifest or locale data for extension at ", edir));
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
        DebugLog.LogEvent($"GetBrowserExtensions() completed. Total runtime: {(DateTime.Now - start).TotalMilliseconds}", DebugLog.Region.System);
        return Browsers;
    }


}