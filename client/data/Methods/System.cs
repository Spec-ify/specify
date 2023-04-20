using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
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

namespace specify_client.data;

public static partial class Cache
{
    //[CLEANUP] add using statement to simplify System.Threading.Tasks.Task to something easier to read.
    public static async System.Threading.Tasks.Task MakeSystemData()
    {
        try
        {
            List<System.Threading.Tasks.Task> DebugTasks = new();
            DebugLog.Region region = DebugLog.Region.System;
            await DebugLog.StartRegion(region);

            //[CLEANUP] I think all of these should be async.

            GetEnvironmentVariables();
            DebugTasks.Add(DebugLog.LogEventAsync("Environment Variables retrieved.", region));

            GetSystemWMIInfo();
            DebugTasks.Add(DebugLog.LogEventAsync("System WMI Information retrieved.", region));

            CheckCommercialOneDrive();

            InstalledApps = GetInstalledApps();
            DebugTasks.Add(DebugLog.LogEventAsync("InstalledApps Information retrieved.", region));

            ScheduledTasks = GetScheduledTasks();
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

            DumpZip = await GetMiniDumps();
            DebugTasks.Add(DebugLog.LogEventAsync("Minidump gathering complete", region));

            DefaultBrowser = GetDefaultBrowser();
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
            await DebugLog.LogFatalError($"{ex}", DebugLog.Region.System);
        }
    }

    private static List<OutputProcess> GetProcesses()
    {
        DateTime start = DateTime.Now;
        DebugLog.LogEvent("GetProcesses() started", DebugLog.Region.System);
        var outputProcesses = new List<OutputProcess>();
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
                        DebugLog.LogEvent($"System Data: Could not get the EXE path of {rawProcess.ProcessName} ({rawProcess.Id})", DebugLog.Region.System, DebugLog.EventType.WARNING);
                    }
                    else exePath = "SYSTEM";
                }
                else exePath = sb.ToString();
            }
            catch (Win32Exception e)
            {
                exePath = "null - See Debug Log.";
                DebugLog.LogEvent($"System Data: Could not get the EXE path of {rawProcess.ProcessName} ({rawProcess.Id})", DebugLog.Region.System, DebugLog.EventType.ERROR);
                DebugLog.LogEvent($"{e}", DebugLog.Region.System);
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

        DebugLog.LogEvent($"GetProcesses() completed. Total runtime: {(DateTime.Now - start).TotalMilliseconds}", DebugLog.Region.System);
        return outputProcesses;
    }

    private static List<InstalledApp> GetInstalledApps()
    {
        // Code Adapted from https://social.msdn.microsoft.com/Forums/en-US/94c2f14d-c45e-4b55-9ba0-eb091bac1035/c-get-installed-programs, thanks Rajasekhar.R! - K97i
        // Currently throws a hissy fit, NullReferenceException when actually adding to the Class

        List<InstalledApp> InstalledApps = new List<InstalledApp>();

        var hckuList = GetInstalledAppsAtKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", Registry.CurrentUser);
        var lm32List = GetInstalledAppsAtKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", Registry.LocalMachine);
        var lm64List = GetInstalledAppsAtKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", Registry.LocalMachine);
        InstalledApps.AddRange(hckuList);
        InstalledApps.AddRange(lm32List);
        InstalledApps.AddRange(lm64List);

        return InstalledApps;
    }

    private static List<InstalledApp> GetInstalledAppsAtKey(string keyLocation, RegistryKey reg)
    {
        var InstalledApps = new List<InstalledApp>();

        var key = reg.OpenSubKey(keyLocation);
        if (key is null)
        {
            DebugLog.LogEvent($"Registry Read Error @ {keyLocation}", DebugLog.Region.System, DebugLog.EventType.ERROR);
            return InstalledApps;
        }
        foreach (String keyName in key.GetSubKeyNames())
        {
            RegistryKey subkey = key.OpenSubKey(keyName);
            var appName = subkey.GetValue("DisplayName") as string;
            var appVersion = subkey.GetValue("DisplayVersion") as string;
            var appDate = subkey.GetValue("InstallDate") as string;

            if (appName == null)
            {
                //DebugLog.LogEvent($"null app name found @ {keyLocation}", DebugLog.Region.System, DebugLog.EventType.ERROR);
                continue;
            }

            //[CLEANUP]: I'm not sure these checks are necessary.
            if (string.IsNullOrEmpty(appVersion))
            {
                appVersion = "";
            }
            if (string.IsNullOrEmpty (appDate))
            {
                appDate = "";
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

    public static async System.Threading.Tasks.Task<StartupTask> CreateStartupTask(string appName, string imagePath)
    {
        StartupTask startupTask = new();
        startupTask.AppName = appName;
        if (string.IsNullOrEmpty(imagePath))
        {
            startupTask.ImagePath = "Image Path not found";
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
    // Group 1: HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
    // Group 2: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
    // Group 3: HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run
    // Group 4: %AppData%\Microsoft\Windows\Start Menu\Programs\Startup
    public static async System.Threading.Tasks.Task<List<StartupTask>> GetStartupTasks()
    {
        DateTime start = DateTime.Now;
        await DebugLog.LogEventAsync("GetStartupTasks() Started", DebugLog.Region.System);
        List<StartupTask> startupTasks = new();

        var group1TaskList = await GetStartupTasksAtKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Registry.CurrentUser);
        var group2TaskList = await GetStartupTasksAtKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Registry.LocalMachine);
        var group3TaskList = await GetStartupTasksAtKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", Registry.LocalMachine);
        var group4TaskList = await GetStartupTasksAtAppData();

        startupTasks.AddRange(group1TaskList);
        startupTasks.AddRange(group2TaskList);
        startupTasks.AddRange(group3TaskList);
        startupTasks.AddRange(group4TaskList);

        await DebugLog.LogEventAsync($"GetStartupTasks() Completed. Total Runtime: {(DateTime.Now - start).TotalMilliseconds}", DebugLog.Region.System);
        return startupTasks;
    }

    private static async System.Threading.Tasks.Task<List<StartupTask>> GetStartupTasksAtKey(string keyLocation, RegistryKey reg)
    {
        List<StartupTask> startupTasks = new();
        var key = reg.OpenSubKey(keyLocation);
        foreach (var appName in key.GetValueNames())
        {
            var startupTask = await CreateStartupTask(appName, (string)key.GetValue(appName));
            startupTasks.Add(startupTask);
        }
        return startupTasks;
    }

    private static async System.Threading.Tasks.Task<List<StartupTask>> GetStartupTasksAtAppData()
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
            await DebugLog.LogEventAsync($"File Read error in group 4 of GetStartupTasks", DebugLog.Region.System, DebugLog.EventType.ERROR);
            await DebugLog.LogEventAsync($"{ex}", DebugLog.Region.System);
        }
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
        var filePath = startupTask.ImagePath.Trim('\"');

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

    public static async System.Threading.Tasks.Task<string> GetMiniDumps()
    {
        DateTime start = DateTime.Now;
        string result = null;
        const string specifiedDumpDestination = "https://dumpload.spec-ify.com/";
        const string dumpDir = @"C:\Windows\Minidump";
        string TempFolder = Path.GetTempPath() + @"specify-dumps";
        string TempZip = Path.GetTempPath() + @"specify-dumps.zip";

        if (!MinidumpsExist(dumpDir))
            return result;

        string[] dumps = Directory.GetFiles(dumpDir);
        if (dumps.Length == 0)
            return result;

        await DebugLog.LogEventAsync("Dump Upload Requested.", DebugLog.Region.System);
        if (MessageBox.Show("Would you like to upload your BSOD minidumps with your specs report?", "Minidumps detected", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            return result;

        await DebugLog.LogEventAsync("Dump Upload Request Approved.", DebugLog.Region.System);

        Directory.CreateDirectory(TempFolder);

        if (!await CreateMinidumpZipFile(dumps, TempFolder, TempZip))
            return result;

        await DebugLog.LogEventAsync("Dump zip file built. Attempting upload.", DebugLog.Region.System);

        result = await UploadMinidumps(TempZip, specifiedDumpDestination);
        if (string.IsNullOrEmpty(result))
            return result;

        await DebugLog.LogEventAsync($"Dump file upload result: {result ?? "null"}", DebugLog.Region.System);
        File.Delete(TempZip);
        new DirectoryInfo(TempFolder).Delete(true);

        await DebugLog.LogEventAsync($"GetMiniDumps() Completed. Total Runtime: {(DateTime.Now - start).TotalMilliseconds}", DebugLog.Region.System);

        return result;
    }

    private static bool MinidumpsExist(string dumpDir)
    {
        if (!Directory.Exists(dumpDir)) return false;

        //If Minidumps hasn't been written to in a month, it's not going to have a dump newer than a month inside of it.
        if (new DirectoryInfo(dumpDir).LastWriteTime < DateTime.Now.AddMonths(-1))
            return false;

        return true;
    }

    private static async System.Threading.Tasks.Task<bool> CreateMinidumpZipFile(string[] dumps, string TempFolder, string TempZip)
    {
        try
        {
            bool copied = false;
            //Any dump older than a month is not included in the zip.
            foreach (string dump in dumps)
            {
                if (new FileInfo(dump).CreationTime > DateTime.Now.AddMonths(-1))
                {
                    var fileName = string.Concat(TempFolder + @"/", Regex.Match(dump, "[^\\\\]*$").Value);
                    File.Copy(dump, fileName);
                    if (!File.Exists(fileName))
                    {
                        await DebugLog.LogEventAsync($"Failed to copy {Regex.Match(dump, "[^\\\\]*$").Value} to dump folder.", DebugLog.Region.System, DebugLog.EventType.ERROR);
                    }
                    else
                    {
                        copied = true;
                    }
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
            await DebugLog.LogEventAsync($"Error occured manipulating dump files! Is this running as admin?", DebugLog.Region.System, DebugLog.EventType.ERROR);
            await DebugLog.LogEventAsync($"{e}", DebugLog.Region.System);

            return false; //If this failed, there's nothing more that can be done here.
        }
    }

    private static async System.Threading.Tasks.Task<string> UploadMinidumps(string TempZip, string specifiedDumpDestination)
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
                await DebugLog.LogEventAsync($"Error occured when uploading dumps.zip to Specified!", DebugLog.Region.System, DebugLog.EventType.ERROR);
                await DebugLog.LogEventAsync($"{e}", DebugLog.Region.System);
            }

            client.Dispose();
        }
        return result;
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

            if (lastWriteTime > DateTime.Now.AddMonths(-1))
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

            // Defender 1
            var disableAv = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender", "DisableAntiVirus");
            var disableAs = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender", "DisableAntiSpyware");
            var passiveMode = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender", "PassiveMode");
            var puaProtection = new RegistryValue<int?>
                (Registry.LocalMachine, @"\SOFTWARE\Microsoft\Windows Defender", "PUAProtection");
            // Defender 2
            var disableAvpolicy = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiVirus");
            var disableAspolicy = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware");
            var passiveModepolicy = new RegistryValue<int?>
                (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "PassiveMode");
            var puaProtectionpolicy = new RegistryValue<int?>
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
                tdrLevel, nbFLimit, throttlingIndex, superFetch, disableAv, disableAs, puaProtection, passiveMode, disableAvpolicy, disableAspolicy,
                puaProtectionpolicy, passiveModepolicy, drii, disableWer,unsupportedTpmOrCpu, hwSchMode, WUServer, noAutoUpdate, fastBoot, auditBoot,
                previewBuilds, bypassCpuCheck, bypassStorageCheck, bypassRamCheck, bypassTpmCheck, bypassSecureBootCheck, hwNotificationCache
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

    private static void CheckCommercialOneDrive()
    {
        bool ODFound = false;
        try
        {
            if (UserVariables["OneDriveCommercial"] is string pathOneDriveCommercial)
            {
                var actualOneDriveCommercial =
                    pathOneDriveCommercial.Split(new string[] { "OneDrive - " }, StringSplitOptions.None)[1];
                OneDriveCommercialPathLength = pathOneDriveCommercial.Length;
                OneDriveCommercialNameLength = actualOneDriveCommercial.Length;
                DebugLog.LogEvent("OneDriveCommercial information retrieved.", DebugLog.Region.System);
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
                    DebugLog.LogEvent("RedactOneDriveCommercial setting disabled. OneDriveCommercial variable not found.", DebugLog.Region.System, DebugLog.EventType.WARNING);
                }
                else
                {
                    DebugLog.LogEvent("OneDriveCommercial variable not found.", DebugLog.Region.System);
                }
            }
        }
    }

    private static List<ScheduledTask> GetScheduledTasks()
    {
        var scheduledTasks = new List<ScheduledTask>();
        using var ts = new TaskService();
        var rawTaskList = EnumScheduledTasks(ts.RootFolder);

        WinScheduledTasks = new List<ScheduledTask>();
        foreach (Task task in rawTaskList)
            if (task.Path.StartsWith("\\Microsoft"))
                WinScheduledTasks.Add(new ScheduledTask(task));
            else
                scheduledTasks.Add(new ScheduledTask(task));

        return scheduledTasks;
    }

    private static string GetDefaultBrowser()
    {
        string defaultBrowserProcess = Regex.Match(Utils.GetRegistryValue<string>(Registry.ClassesRoot, string.Concat(Utils.GetRegistryValue<string>(Registry.CurrentUser,
            "Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\https\\UserChoice", "ProgID"), "\\shell\\open\\command"), ""), "\\w*.exe").Value;
        return (defaultBrowserProcess.Equals("Launcher.exe")) ? "OperaGX" : defaultBrowserProcess;
    }

    private static List<Dictionary<string, object>> GetPowerProfiles()
    {
        try
        {
            return Utils.GetWmi("Win32_PowerPlan", "*", @"root\cimv2\power");
        }
        catch (COMException)
        {
            Issues.Add("Could not get power profiles");
            //[CLEANUP] Will an empty list break something or should it return null?
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
        Services = Utils.GetWmi("Win32_Service", "Name, Caption, PathName, StartMode, State");
        InstalledHotfixes = Utils.GetWmi("Win32_QuickFixEngineering", "Description,HotFixID,InstalledOn");
        // As far as I can tell, Size is the size of the file on the filesystem and Usage is the amount actually used
        PageFile = Utils.GetWmi("Win32_PageFileUsage", "AllocatedBaseSize, Caption, CurrentUsage, PeakUsage").FirstOrDefault();

        PowerProfiles = GetPowerProfiles();
    }
}