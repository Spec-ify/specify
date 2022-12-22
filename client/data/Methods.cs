using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace specify_client.data;

public static partial class Cache
{
    public static void MakeMainData()
    {
        Os = Utils.GetWmi("Win32_OperatingSystem").First();
        Cs = Utils.GetWmi("Win32_ComputerSystem").First();
    }

    public static void DummyTimer()
    {
        Thread.Sleep(5000);
    }

    public static void MakeSystemData()
    {

        SystemVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
        UserVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
        Services = Utils.GetWmi("Win32_Service", "Name, Caption, PathName, StartMode, State");
        InstalledApps = GetInstalledApps();
        InstalledHotfixes = Utils.GetWmi("Win32_QuickFixEngineering", "Description,HotFixID,InstalledOn");
        var ts = new TaskService();
        var rawTaskList = EnumScheduledTasks(ts.RootFolder);
        ScheduledTasks = rawTaskList.Select(e => new ScheduledTask(e)).ToList();
        RunningProcesses = new List<OutputProcess>();
        ChoiceRegistryValues = RegistryCheck();
        PowerProfiles = Utils.GetWmi("Win32_PowerPlan", "*", @"root\cimv2\power");
        MicroCodeCheck = CheckForMicroCode();
        MinidumpCount = CountMinidumps();
        StaticCoreCheck = CheckStaticCore();
        var rawProcesses = Process.GetProcesses();

        foreach (var rawProcess in rawProcesses)
        {
            double cpuPercent = -1.0; // TODO: make this actually work properly
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

            RunningProcesses.Add(new OutputProcess
            {
                ProcessName = rawProcess.ProcessName,
                ExePath = exePath,
                Id = rawProcess.Id,
                WorkingSet = rawProcess.WorkingSet64,
                CpuPercent = cpuPercent
            });
        }
    }
    public static List<MicroCode> CheckForMicroCode()
    {
        List<MicroCode> Check = new List<MicroCode>();

        var IntelPath = "C:\\Windows\\System32\\mcupdate_genuineintel.dll";
        var AMDPath = "C:\\Windows\\System32\\mcupdate_authenticamd.dll";

        Check.Add(
                new MicroCode
                {
                    Name = IntelPath,
                    Exists = File.Exists(IntelPath)
                });

        Check.Add(
            new MicroCode
            {
                Name = AMDPath,
                Exists = File.Exists(AMDPath)
            });

        return Check;
    }
    public static List<StaticCore> CheckStaticCore()
    {
        List<StaticCore> Cores = new List<StaticCore>();

        string output = string.Empty;

        ProcessStartInfo procStartInfo = new ProcessStartInfo("bcdedit", "/enum");
        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.UseShellExecute = false;
        procStartInfo.CreateNoWindow = true;

        using (Process proc = new Process())
        {
            proc.StartInfo = procStartInfo;
            proc.Start();
            output = proc.StandardOutput.ReadToEnd();
        }

        if (output.Contains("numproc"))
        {
            Cores.Add(
                new StaticCore
                {
                On = true
                });
        }
        else
        {
            Cores.Add(
                new StaticCore
                {
                    On = false
                });
        }

        return Cores;
    }
    public static List<Minidump> CountMinidumps()
    {
        List<Minidump> Count = new List<Minidump>();

        var DumpPath = "C:\\Windows\\Minidump";

        string[] files = System.IO.Directory.GetFiles(DumpPath);

        int count = 0;

        foreach (string file in files)
        {
            DateTime lastWriteTime = System.IO.File.GetLastWriteTime(file);

            if (lastWriteTime > DateTime.Now.AddDays(-7))
            {
                count++;
            }

        }

        Count.Add(
            new Minidump
            {
                Count = count
            });

        return Count;
    }

    public static List<IRegistryValue> RegistryCheck()
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
    
    private static List<Task> EnumScheduledTasks(TaskFolder fld)
    {
        var res = fld.Tasks.ToList();
        foreach (var sfld in fld.SubFolders)
            res.AddRange(EnumScheduledTasks(sfld));

        return res;
    }

    public static void MakeHardwareData()
    {
        Cpu = Utils.GetWmi("Win32_Processor",
            "CurrentClockSpeed, Manufacturer, Name, SocketDesignation").First();
        Gpu = Utils.GetWmi("Win32_VideoController",
            "Description, AdapterRam, CurrentHorizontalResolution, CurrentVerticalResolution, "
            + "CurrentRefreshRate, CurrentBitsPerPixel");
        Motherboard = Utils.GetWmi("Win32_BaseBoard", "Manufacturer, Product, SerialNumber").First();
        AudioDevices = Utils.GetWmi("Win32_SoundDevice", "Name, Manufacturer, Status, DeviceID");
        MonitorInfo = GetMonitorInfo();
        Drivers = Utils.GetWmi("Win32_PnpSignedDriver", "FriendlyName,Manufacturer,DeviceID,DeviceName,DriverVersion");
        Devices = Utils.GetWmi("Win32_PnpEntity", "DeviceID,Name,Description,Status");
        BiosInfo = Utils.GetWmi("Win32_bios");
        Ram = GetSMBiosMemoryInfo();
        Disks = GetDiskDriveData();
        Temperatures = GetTemps();
        Batteries = GetBatteryData();
    }

    public static void MakeSecurityData()
    {
        AvList = Utils.GetWmi("AntivirusProduct", "displayName", @"root\SecurityCenter2")
            .Select(x => (string)x["displayName"]).ToList();
        FwList = Utils.GetWmi("FirewallProduct", "displayName", @"root\SecurityCenter2")
            .Select(x => (string)x["displayName"]).ToList();

        var enableLua = Utils.GetRegistryValue<int?>(Registry.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA");
        if (enableLua == null) Issues.Add($"Security data: could not get EnableLUA value");
        else UacEnabled = enableLua == 1;

        if (Environment.GetEnvironmentVariable("firmware_type").Equals("UEFI"))
        {
            var secBootEnabled = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SecureBoot\State",
                "UEFISecureBootEnabled");

            if (secBootEnabled == null) Issues.Add($"Security data: could not get UEFISecureBootEnabled value");
            else SecureBootEnabled = secBootEnabled == 1;
        }

        try
        {
            Tpm = Utils.GetWmi("Win32_Tpm", "*", @"Root\CIMV2\Security\MicrosoftTpm").First();
            Tpm["IsPresent"] = true;
        }
        catch (InvalidOperationException)
        {
            // No TPM
            Tpm = new Dictionary<string, object>() { { "IsPresent", false } };
        }
        catch (ManagementException)
        {
            Tpm = null;
            Issues.Add("Security Data: could not get TPM. This is probably because specify was not run as administrator.");
        }

        UacLevel = Utils.GetRegistryValue<int?>(
            Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            "ConsentPromptBehaviorAdmin");
    }
    public static void MakeNetworkData()
    {
        NetAdapters = Utils.GetWmi("Win32_NetworkAdapterConfiguration",
            "Description, DHCPEnabled, DHCPServer, DNSDomain, DNSDomainSuffixSearchOrder, DNSHostName, "
            + "DNSServerSearchOrder, IPEnabled, IPAddress, IPSubnet, DHCPLeaseObtained, DHCPLeaseExpires, "
            + "DefaultIPGateway, MACAddress, InterfaceIndex");
        IPRoutes = Utils.GetWmi("Win32_IP4RouteTable",
            "Description, Destination, Mask, NextHop, Metric1, InterfaceIndex");
        HostsFile = File.ReadAllText(@"C:\Windows\system32\drivers\etc\hosts");
        NetworkConnections = GetNetworkConnections();

        // Uncomment the block below to run a traceroute to Google's DNS
        /*var NetStats = await GetNetworkRoutes("8.8.8.8", 1000);
        for (int i = 0; i < NetStats.Address.Count; i++)
        {
            Console.WriteLine($"{i}: {NetStats.Address[i]} --- Lat: {NetStats.AverageLatency[i]} --- PL: {NetStats.PacketLoss[i]}");
        }*/
    }
    private static async System.Threading.Tasks.Task<NetworkRoute> GetNetworkRoutes(string ipAddress, int pingCount = 100, int timeout = 10000, int bufferSize = 100)
    {
        var addressList = GetTraceroute(ipAddress, timeout, 30, bufferSize);
        var networkRoute = new NetworkRoute();

        foreach (var address in addressList)
        {
            networkRoute.Address.Add(address.ToString());
            (int Latency, double Loss) hostStats = await GetHostStats(ipAddress, timeout, pingCount);
            networkRoute.AverageLatency.Add(hostStats.Latency);
            networkRoute.PacketLoss.Add(hostStats.Loss);
        }

        return networkRoute;
    }

    private static async System.Threading.Tasks.Task<(int, double)> GetHostStats(string ipAddress, int timeout = 10000, int pingCount = 100)
    {
        var pinger = new Ping();
        var pingOptions = new PingOptions();

        var data = "meaninglessdatawithalotofletters"; // 32 letters in total.
        var buffer = Encoding.ASCII.GetBytes(data);

        var failedPings = 0;
        var errors = 0;
        var latencySum = 0;
        var statTasks = new List<System.Threading.Tasks.Task<int>>();

        for (int i = 0; i < pingCount; i++)
        {
            var task = System.Threading.Tasks.Task.Run(() => GetLatency(ipAddress, timeout, buffer, pingOptions));
            statTasks.Add(task);
        }
        await System.Threading.Tasks.Task.WhenAll(statTasks);
        foreach (var task in statTasks)
        {
            switch (task.Result)
            {
                case -1:
                    failedPings++;
                    break;
                case -2:
                    errors++;
                    break;
                default:
                    latencySum += task.Result;
                    break;
            }
        }
        var averageLatency = latencySum / pingCount;
        var packetLoss = failedPings / (double)pingCount;
        if (errors > 0)
        {
            Console.WriteLine($"{ipAddress} - ERRORS: {errors}");
        }
        return (averageLatency, packetLoss);
    }
    private static async System.Threading.Tasks.Task<int> GetLatency(string ipAddress, int timeout, byte[] buffer, PingOptions pingOptions)
    {
        try
        {
            var pingReply = await new Ping().SendPingAsync(ipAddress, timeout, buffer, pingOptions);

            if (pingReply != null)
            {
                if (pingReply.Status != IPStatus.Success)
                {
                    return -1;
                }
                else
                {
                    return (int)pingReply.RoundtripTime;
                }
            }
            else
            {
                return -1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return -2;
        }
    }
    private static IEnumerable<IPAddress> GetTraceroute(string ipAddress, int timeout = 10000, int maxTTL = 30, int bufferSize = 100)
    {
        // Cap off the TTL to not overdo the basal traceroute.
        if (maxTTL > 30)
        {
            maxTTL = 30;
        }

        var buffer = new byte[bufferSize];
        new Random().NextBytes(buffer);

        using var pingTool = new Ping();

        foreach (var i in Enumerable.Range(1, maxTTL))
        {
            var pingOptions = new PingOptions(i, true);
            var reply = pingTool.Send(ipAddress, timeout, buffer, pingOptions);

            if (reply.Status is IPStatus.Success or IPStatus.TtlExpired)
            {
                yield return reply.Address;
            }
            if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
            {
                break;
            }
        }
    }
    public static List<Monitor> GetMonitorInfo()
    {
        List<Monitor> MonitorInfo = new List<Monitor>();
        String path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

        Process cmd = new Process
        {
            StartInfo =
            {
                FileName = "cmd",
                WorkingDirectory = path,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                Arguments = "/Q /C dxdiag /x dxinfo.xml"
            }
        };

        if (File.Exists(Path.Combine(path, "dxinfo.xml")))
        {
            File.Delete(Path.Combine(path, "dxinfo.xml"));
        }

        cmd.Start();

        Stopwatch timer = Stopwatch.StartNew();
        TimeSpan timeout = new TimeSpan().Add(TimeSpan.FromSeconds(60));
            
        while (timer.Elapsed < timeout)

            if (File.Exists(Path.Combine(path, "dxinfo.xml")) && Process.GetProcessesByName("dxdiag").Length == 0)
                {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(path, "dxinfo.xml"));
                List<JToken> Monitor = JObject.Parse(JsonConvert.SerializeXmlNode(doc))["DxDiag"]["DisplayDevices"].Children().Children().ToList();

                var videoid = 0;

                // very inefficient while loop right here, but as long as it works, thats what matters -K97i
                while (true)
                {

                    try
                    {
                        foreach (JToken DisplayDevice in Monitor)

                            if (DisplayDevice.HasValues)
                            {

                                MonitorInfo.Add(
                                    new Monitor
                                    {
                                        Name = (string)DisplayDevice[videoid]["CardName"],
                                        ChipType = (string)DisplayDevice[videoid]["ChipType"],
                                        DedicatedMemory = (string)DisplayDevice[videoid]["DedicatedMemory"],
                                        MonitorModel = (string)DisplayDevice[videoid]["MonitorModel"],
                                        CurrentMode = (string)DisplayDevice[videoid]["CurrentMode"]
                                    });

                                videoid++;

                            }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }

                    catch (ArgumentException)
                    {
                        foreach (JToken DisplayDevice in Monitor)

                            if (DisplayDevice.HasValues)
                            {

                                MonitorInfo.Add(
                                    new Monitor
                                    {
                                        Name = (string)DisplayDevice["CardName"],
                                        ChipType = (string)DisplayDevice["ChipType"],
                                        DedicatedMemory = (string)DisplayDevice["DedicatedMemory"],
                                        MonitorModel = (string)DisplayDevice["MonitorModel"],
                                        CurrentMode = (string)DisplayDevice["CurrentMode"]
                                    });

                                break;
                            }
                        break;
                    }
                }

                break;
            }
        if (timer.Elapsed > timeout)
            Issues.Add("Monitor report was not generated before the timeout!");

        timer.Stop();
        cmd.Close();

        File.Delete(Path.Combine(path, "dxinfo.xml"));

        return MonitorInfo;
    }
    private static List<DiskDrive> GetDiskDriveData()
    {
        List<DiskDrive> drives = new List<DiskDrive>();

        var driveWmiInfo = Utils.GetWmiObj("Win32_DiskDrive");

        // This assumes the WMI info reports disks in order by drive number. I'm not certain this is a safe assumption.
        var diskNumber = 0;
        foreach (var driveWmi in driveWmiInfo)
        {
            DiskDrive drive = new();
            try
            {
                drive.DeviceName = ((string)driveWmi["Model"]).Trim();
            }
            catch (NullReferenceException)
            {
                drive.DeviceName = null;
                Issues.Add($"Could not retrieve device name of drive @ index {diskNumber}");
            }
            try
            {
                drive.SerialNumber = ((string)driveWmi["SerialNumber"]).Trim();
            }
            catch (NullReferenceException)
            {
                drive.SerialNumber = null;
                Issues.Add($"Could not retrieve serial number of drive @ index {diskNumber}");
            }

            drive.DiskNumber = (UInt32)driveWmi["Index"];

            try
            {
                drive.DiskCapacity = (ulong)driveWmi["Size"];
            }
            catch (NullReferenceException)
            {
                drive.DiskCapacity = null;
                Issues.Add($"Could not retrieve capacity of drive @ index {diskNumber}");
            }
            try
            {
                drive.InstanceId = (string)driveWmi["PNPDeviceID"];
            }
            catch (NullReferenceException)
            {
                drive.InstanceId = null;
                Issues.Add($"Could not retrieve Instance ID of drive @ index {diskNumber}");
            }

            drive.Partitions = new List<Partition>();

            diskNumber++;
            drives.Add(drive);
        }

        var partitionWmiInfo = Utils.GetWmiObj("Win32_DiskPartition");
        foreach (var partitionWmi in partitionWmiInfo)
        {
            var partition = new Partition()
            {
                PartitionCapacity = (UInt64)partitionWmi["Size"],
                Caption = (string)partitionWmi["Caption"]
            };
            var diskIndex = (UInt32)partitionWmi["DiskIndex"];

            foreach (var disk in drives)
            {
                if (disk.DiskNumber == diskIndex)
                {
                    disk.Partitions.Add(partition);
                    break;
                }
            }
        }
        try
        {
            var queryCollection = Utils.GetWmiObj("MSStorageDriver_FailurePredictData", "*", "\\\\.\\root\\wmi");
            foreach (var m in queryCollection)
            {

                // The following lines up to the attribute list creationlink smart data to its corresponding drive.
                // It makes the assumption that the PNPDeviceID in Wmi32_DiskDrive has a matching identification code to MSStorageDriver_FailurePredictData's InstanceID, and that these identification codes are unique.
                // This is not a safe assumption, testing will be required.
                var instanceId = (string)m["InstanceName"];
                instanceId = instanceId.Substring(0, instanceId.Length - 2);
                var splitID = instanceId.Split('\\');
                instanceId = splitID[splitID.Count() - 1];

                var driveIndex = -1;
                for (var i = 0; i < drives.Count; i++)
                {
                    var drive = drives[i];
                    if (!drive.InstanceId.ToLower().Contains(instanceId.ToLower())) continue;
                    driveIndex = i;
                    break;
                }

                if (driveIndex == -1)
                {
                    Issues.Add($"Smart Data found for {instanceId} with no matching drive. This is a Specify error");
                    break;
                }

                var diskAttributes = new List<SmartAttribute>();

                var vs = (byte[])m["VendorSpecific"];
                // Every 12th byte starting at byte index 2 is a smart identifier.
                for (var i = 2; i < vs.Length; i += 12)
                {
                    var c = new byte[12];
                    // Copy 12 bytes into a new array.
                    Array.Copy(vs, i, c, 0, 12);

                    // Once we reach the zeroes, we're past the smart attributes.
                    if (c[0] == 0)
                    {
                        break;
                    }
                    diskAttributes.Add(GetAttribute(c));
                }
                drives[driveIndex].SmartData = diskAttributes;
            }
        }
        catch (ManagementException e)
        {
            Issues.Add("Error retrieving SMART Utils." + e.Message);
        }
        var LDtoP = Utils.GetWmiObj("Win32_LogicalDiskToPartition");
        for (var di = 0; di < drives.Count(); di++)
        {
            for (var pi = 0; pi < drives[di].Partitions.Count(); pi++)
            {
                foreach (var logicalDisk in LDtoP)
                {
                    try
                    {
                        if (((string)logicalDisk["Antecedent"]).Contains(drives[di].Partitions[pi].Caption))
                        {
                            var dependent = (string)logicalDisk["Dependent"];
                            var whatIActuallyWantFromDependent = dependent.Split('"')[1].Replace("\\", string.Empty);
                            var runningOutOfVariableNames = Utils.GetWmiObj("Win32_LogicalDisk");
                            foreach (var lackingCreativity in runningOutOfVariableNames)
                            {
                                if (whatIActuallyWantFromDependent == (string)lackingCreativity["DeviceID"])
                                {
                                    drives[di].Partitions[pi].PartitionLabel = whatIActuallyWantFromDependent;
                                    drives[di].Partitions[pi].PartitionFree = (UInt64)lackingCreativity["FreeSpace"];
                                    drives[di].Partitions[pi].Filesystem = (string)lackingCreativity["FileSystem"];
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        throw new Exception("CATCH POINT ONE");
                    }
                }
            }
        }
        var partitionInfo = Utils.GetWmiObj("Win32_Volume");
        foreach (var partition in partitionInfo)
        {
            // Check if partition drive size is identical to exactly one partition drive size in the list of disks. If it is, add win32_volume data to it.
            // If it is not, create an issue for the failed link.
            ulong partitionSize = 0;
            ulong blockSize = 0;
            try
            {
                partitionSize = (ulong)partition["Capacity"];
                blockSize = (ulong)partition["BlockSize"];
            }
            catch (NullReferenceException)
            {
                Issues.Add("Failure to parse partition information - No capacity found. This is likely a virtual or unallocated drive.");
                continue;
            }

            // Drive and Partition indices.
            var dIndex = -1;
            var pIndex = -1;

            // Indicators; If found and unique, store partition information with the drive under dIndex/pIndex.
            // Otherwise, store into non-specific partition list.
            var found = false;
            var unique = true;
            //var LDtoP = Utils.GetWmiObj("Win32_LogicalDiskToPartition");
            for (var di = 0; di < drives.Count(); di++)
            {
                for (var pi = 0; pi < drives[di].Partitions.Count(); pi++)
                {
                    if(drives[di].Partitions[pi].Filesystem != null)
                    {
                        continue;
                    }    
                    var fileSystem = (string)partition["FileSystem"];
                    if (fileSystem.ToLower().Equals("ntfs"))
                    {
                        if (Math.Abs((float)partitionSize - drives[di].Partitions[pi].PartitionCapacity) > 8192)
                            continue;
                        // If it hasn't been found yet, this is a potential match.
                        if (!found)
                        {
                            pIndex = pi;
                            dIndex = di;
                            found = true;
                        }
                        // If it has been found, there are two matches, it is not unique, stop the check.
                        else
                        {
                            unique = false;
                            break;
                        }
                    }
                    else if (fileSystem.ToLower().Equals("fat32") || fileSystem.ToLower().Equals("exfat32"))
                    {
                        if (partitionSize != drives[di].Partitions[pi].PartitionCapacity &&
                            partitionSize + (2048 * 2048) != drives[di].Partitions[pi].PartitionCapacity &&
                            partitionSize - (2048 * 2048) != drives[di].Partitions[pi].PartitionCapacity) continue;
                        // If it hasn't been found yet, this is a potential match.
                        if (!found)
                        {
                            pIndex = pi;
                            dIndex = di;
                            found = true;
                        }
                        // If it has been found, there are two matches, it is not unique, stop the check.
                        else
                        {
                            unique = false;
                            break;
                        }
                    }
                }
                // If it is not unique, no drive or partition index is valid. Stop checking.
                if (unique) 
                    continue;
                dIndex = -1;
                pIndex = -1;
                break;
            }
            if (found && unique)
            {
                // These should never be -1, however they seem to happen occasionally.
                // Prevent the exception by continuing the loop.
                if (dIndex == -1 || pIndex == -1)
                {
                    Issues.Add($"di/pi = -1 for partition {partitionSize} - this is a Specify error.");
                    continue;
                }
                var matchingPartition = drives[dIndex].Partitions[pIndex];
                var driveLetter = partition["DriveLetter"];
                if (driveLetter != null)
                {
                    matchingPartition.PartitionLabel = (string)driveLetter;
                }
                matchingPartition.PartitionFree = (ulong)partition["FreeSpace"];
                var fileSystem = partition["FileSystem"];
                if (fileSystem != null)
                {
                    matchingPartition.Filesystem = (string)fileSystem;
                }
                Issues.Add($"{partitionSize} found. FS: {fileSystem} - DL: {driveLetter}");
            }
            else
            {
                var driveLetter = "";
                var fileSystem = "";
                try
                {
                    driveLetter = (string)partition["DriveLetter"];
                    fileSystem = (string)partition["FileSystem"];
                }
                catch
                { }
                Issues.Add($"Partition link could not be established for {partitionSize} byte partition - Drive Label: {driveLetter} -  File System: {fileSystem}");
            }
        }
        foreach (var d in drives)
        {
            bool complete = true;
            UInt64 free = 0;
            foreach (var partition in d.Partitions)
            {
                if (partition.PartitionFree == 0)
                {
                    complete = false;
                }
                else
                {
                    free += partition.PartitionFree;
                }
            }
            if (!complete)
            {
                var LetteredDrives = DriveInfo.GetDrives();
                foreach (var letteredDrive in LetteredDrives)
                {
                    foreach(var partition in d.Partitions)
                    {
                        try
                        {
                            if (letteredDrive.Name.Contains(partition.PartitionLabel[0]))
                            {
                                d.DiskFree = (UInt64)letteredDrive.AvailableFreeSpace;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            else
            {
                d.DiskFree = free;
            }
        }
        return drives;
    }
    private static SmartAttribute GetAttribute(byte[] data)
    {
        // Smart data is fed backwards, with byte 10 being the first byte for the attribute and byte 5 being the last.
        var values = new byte[6]
        {
            data[10], data[9], data[8], data[7], data[6], data[5]
        };

        var attribute = new SmartAttribute()
        {
            Id = data[0],
            Name = GetAttributeName(data[0]),
        };
        var rawValue = BitConverter.ToString(values);

        rawValue = rawValue.Replace("-", string.Empty);
        attribute.RawValue = rawValue;
        return attribute;
    }
    private static string GetAttributeName(byte id)
    {
        return id switch
        {
            0x1 => "Read Error Rate",
            0x2 => "Throughput Performance",
            0x3 => "Spin-Up Time",
            0x4 => "Start/Stop Count",
            0x5 => "Reallocated Sectors Count(!)",
            0x6 => "Read Channel Margin",
            0x7 => "Seek Error Rate",
            0x8 => "Seek Time Performance",
            0x9 => "Power-On Hours",
            0xA => "Spin Retry Count(!)",
            0xB => "Calibration Retry Count",
            0xC => "Power Cycle Count",
            0xD => "Soft Read Error Rate",
            0x16 => "Current Helium Level",
            0x17 => "Helium Condition Lower",
            0x18 => "Helium Condition Upper",
            0xAA => "Available Reserved Space",
            0xAB => "SSD Program Fail Count",
            0xAC => "SSD Erase Fail Count",
            0xAD => "SSD Wear Leveling Count",
            0xAE => "Unexpected Power Loss Count",
            0xAF => "Power Loss Protection Failure",
            0xB0 => "Erase Fail Count",
            0xB1 => "Wear Range Delta",
            0xB2 => "Used Reserved Block Count",
            0xB3 => "Used Reserved Block Count Total",
            0xB4 => "Unused Reserved Block Count Total",
            0xB5 => "Vendor Specific", // Program Fail Count Total or Non-4K Aligned Access Count
            0xB6 => "Erase Fail Count",
            0xB7 => "Vendor Specific (WD or Seagate)", //SATA Downshift Error Count or Runtime Bad Block. WD or Seagate respectively.
            0xB8 => "End-to-end Error Count(!)",
            0xB9 => "Head Stability",
            0xBA => "Induced Op-Vibration Detection",
            0xBB => "Reported Uncorrectable Errors(!)",
            0xBC => "Command Timeout(!)",
            0xBD => "High Fly Writes(!)",
            0xBE => "Airflow Temperature",
            0xBF => "G-Sense Error Rate",
            0xC0 => "Unsafe Shutdown Count",
            0xC1 => "Load Cycle Count",
            0xC2 => "Temperature",
            0xC3 => "Hardware ECC Recovered",
            0xC4 => "Reallocation Event Count(!)",
            0xC5 => "Current Pending Sector Count(!)",
            0xC6 => "Uncorrectable Sector Count(!)",
            0xC7 => "UltraDMA CRC Error Count",
            0xC8 => "Multi-Zone Error Rate(!)(Unless Fujitsu)",
            0xC9 => "Soft Read Error Rate(!)",
            0xCA => "Data Address Mark Errors",
            0xCB => "Run Out Cancel",
            0xCC => "Soft ECC Correction",
            0xCD => "Thermal Asperity Rate",
            0xCE => "Flying Height",
            0xCF => "Spin High Current",
            0xD0 => "Spin Buzz",
            0xD1 => "Offline Seek Performance",
            0xD2 => "Vibration During Write",
            0xD3 => "Vibration During Write",
            0xD4 => "Shock During Write",
            0xDC => "Disk Shift",
            0xDD => "G-Sense Error Rate",
            0xDE => "Loaded Hours",
            0xDF => "Load/Unload Retry Count",
            0xE0 => "Load Friction",
            0xE1 => "Load/Unload Cycle Count",
            0xE2 => "Load 'In'-time",
            0xE3 => "Torque Amplification Count",
            0xE4 => "Power-Off Retract Cycle",
            0xE6 => "GMR Head Amplitude / Drive Life Protection Status", // HDDs / SSDs respectively.
            0xE7 => "SSD Life Left / HDD Temperature",
            0xE8 => "Vendor Specific", // Endurance Remaining or Available Reserved Space.
            0xE9 => "Media Wearout Indicator",
            0xEA => "Average and Maximum Erase Count",
            0xEB => "Good Block and Free Block Count",
            0xF0 => "Head Flying Hours (Unless Fujitsu)",
            0xF1 => "Total LBAs Written",
            0xF2 => "Total LBAs Read",
            0xF3 => "Total LBAs Written Expanded",
            0xF4 => "Total LBAs Read Expanded",
            0xF9 => "NAND Writes (# of GiB)",
            0xFA => "Read Error Retry Rate",
            0xFB => "Minimum Spares Remaining",
            0xFC => "Newly Added Bad Flash Block",
            0xFE => "Free Fall Protection",
            _ => "Vendor Specific"
        };
        ;
    }
    private static List<RamStick> GetSMBiosMemoryInfo()
    {
        // Made a new GetWmi function because I'm not knowledgeable enough in how this works to translate the Dictionary object into what I need for this.
        var SMBiosObj = Utils.GetWmiObj("MSSMBios_RawSMBiosTables", "*", "root\\WMI");

        // If no data is received, stop before it excepts. Add error message?
        if (SMBiosObj == null)
        {
            Issues.Add("Hardware Data: Could not get SMBios info for RAM.");
            return null;
        }

        // Store raw SMBios Data
        byte[] SMBios = null;
        foreach (ManagementObject obj in SMBiosObj)
        {
            SMBios = (byte[])obj["SMBiosData"];
        }

        var offset = 0;
        var type = SMBios[offset];

        var SMBiosMemoryInfo = new List<RamStick>();

        while (offset + 4 < SMBios.Length && type != 127)
        {
            type = SMBios[offset];
            var dataLength = SMBios[offset + 1];

            // If the data extends the bounds of the SMBios Data array, stop.
            if (offset + dataLength > SMBios.Length)
            {
                break;
            }

            var data = new byte[dataLength];
            Array.Copy(SMBios, offset, data, 0, dataLength);
            offset += dataLength;

            var smbStringsList = new List<string>();

            if (offset < SMBios.Length && SMBios[offset] == 0)
                offset++;

            // Iterate the byte array to build a list of SMBios structures.
            while (offset < SMBios.Length && SMBios[offset] != 0)
            {
                var smbDataString = new System.Text.StringBuilder();
                while (offset < SMBios.Length && SMBios[offset] != 0)
                {
                    smbDataString.Append((char)SMBios[offset]);
                    offset++;
                }
                offset++;
                smbStringsList.Add(smbDataString.ToString());
            }
            offset++;

            // This is the only type we care about; Type 17. If the type is anything else, it simply loops again.
            if (type != 0x11) continue;

            var stick = new RamStick();
            // These if statements confirm the data received is valid data.
            // We don't need else statements here because the default is null
            if (0x10 < data.Length && data[0x10] > 0 && data[0x10] <= smbStringsList.Count)
            {
                stick.DeviceLocation = smbStringsList[data[0x10] - 1].Trim();
            }

            if (0x11 < data.Length && data[0x11] > 0 && data[0x11] <= smbStringsList.Count)
            {
                stick.BankLocator = smbStringsList[data[0x11] - 1].Trim();
            }

            if (0x17 < data.Length && data[0x17] > 0 && data[0x17] <= smbStringsList.Count)
            {
                stick.Manufacturer = smbStringsList[data[0x17] - 1].Trim();
            }

            if (0x18 < data.Length && data[0x18] > 0 && data[0x18] <= smbStringsList.Count)
            {
                stick.SerialNumber = smbStringsList[data[0x18] - 1].Trim();
            }

            if (0x1A < data.Length && data[0x1A] > 0 && data[0x1A] <= smbStringsList.Count)
            {
                stick.PartNumber = smbStringsList[data[0x1A] - 1].Trim();
            }

            if (0x15 + 1 < data.Length)
            {
                stick.ConfiguredSpeed = (data[0x15 + 1] << 8) | data[0x15];
            }

            if (0xC + 1 < data.Length)
            {
                stick.Capacity = (data[0xC + 1] << 8) | data[0xC];
            }
            SMBiosMemoryInfo.Add(stick);
        }
        return SMBiosMemoryInfo;
    }
    private static List<TempMeasurement> GetTemps()
    {
        //Any temp sensor reading below 24 will be filtered out
        //These sensors are either not reading in celsius, are in error, or we cannot interpret them properly here
        var Temps = new List<TempMeasurement>();
        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true
        };

        try
        {
            computer.Open();
            computer.Accept(new SensorUpdateVisitor());

            foreach (var hardware in computer.Hardware)
            {
                Temps.AddRange(
                    from subhardware in hardware.SubHardware
                    from sensor in subhardware.Sensors
                    where sensor.SensorType.Equals(SensorType.Temperature) && sensor.Value > 24
                    select new TempMeasurement
                    { Hardware = hardware.Name, SensorName = sensor.Name, SensorValue = sensor.Value.Value }
                    );

                Temps.AddRange(
                    from sensor in hardware.Sensors
                    where sensor.SensorType.Equals(SensorType.Temperature) && sensor.Value > 24
                    select new TempMeasurement
                    { Hardware = hardware.Name, SensorName = sensor.Name, SensorValue = sensor.Value.Value }
                    );
            }
        }
        catch (OverflowException)
        {
            Issues.Add("Absolute value overflow occured when fetching temperature data");
        }
        finally
        {
            computer.Close();
        }

        return Temps;
    }
    public static List<Interop.MIB_TCPROW_OWNER_PID> GetAllTCPv4Connections()
    {
        return GetTCPConnections<Interop.MIB_TCPROW_OWNER_PID, Interop.MIB_TCPTABLE_OWNER_PID>(AF_INET);
    }

    public static List<Interop.MIB_TCP6ROW_OWNER_PID> GetAllTCPv6Connections()
    {
        return GetTCPConnections<Interop.MIB_TCP6ROW_OWNER_PID, Interop.MIB_TCP6TABLE_OWNER_PID>(AF_INET6);
    }

    public static List<IPR> GetTCPConnections<IPR, IPT>(int ipVersion)
    {

        IPR[] tableRows;
        int buffSize = 0;
        var dwNumEntriesField = typeof(IPT).GetField("dwNumEntries");

        uint ret = Interop.GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, ipVersion, Interop.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
        IntPtr tcpTablePtr = Marshal.AllocHGlobal(buffSize);

        try
        {
            ret = Interop.GetExtendedTcpTable(tcpTablePtr, ref buffSize, true, ipVersion, Interop.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
            if (ret != 0) return new List<IPR>();

            IPT table = (IPT)Marshal.PtrToStructure(tcpTablePtr, typeof(IPT));
            int rowStructSize = Marshal.SizeOf(typeof(IPR));
            uint numEntries = (uint)dwNumEntriesField.GetValue(table);

            tableRows = new IPR[numEntries];

            IntPtr rowPtr = (IntPtr)((long)tcpTablePtr + 4);
            for (int i = 0; i < numEntries; i++)
            {
                IPR tcpRow = (IPR)Marshal.PtrToStructure(rowPtr, typeof(IPR));
                tableRows[i] = tcpRow;
                rowPtr = (IntPtr)((long)rowPtr + rowStructSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }
        return tableRows != null ? tableRows.ToList() : new List<IPR>();
    }
    private static List<NetworkConnection> GetNetworkConnections()
    {
        List<NetworkConnection> connectionsList = new();
        var connections = GetAllTCPv4Connections();
        foreach (var connection in connections)
        {
            NetworkConnection conn = new();
            int port = 0;
            port += connection.localPort[0] << 8;
            port += connection.localPort[1];

            int rport = 0;
            rport += connection.remotePort[0] << 8;
            rport += connection.remotePort[1];

            var la = connection.localAddr;

            uint localAddr1 = la % 256;
            la = la / 256;
            uint localAddr2 = la % 256;
            la = la / 256;
            uint localAddr3 = la % 256;
            uint localAddr4 = la / 256;

            var ra = connection.remoteAddr;
            uint remoteAddr1 = ra % 256;
            ra = ra / 256;
            uint remoteAddr2 = ra % 256;
            ra = ra / 256;
            uint remoteAddr3 = ra % 256;
            uint remoteAddr4 = ra / 256;

            conn.LocalIPAddress = $"{localAddr1}.{localAddr2}.{localAddr3}.{localAddr4}";
            conn.LocalPort = port;
            conn.RemoteIPAddress = $"{remoteAddr1}.{remoteAddr2}.{remoteAddr3}.{remoteAddr4}";
            conn.RemotePort = rport;
            conn.OwningPID = connection.owningPid;

            connectionsList.Add(conn);
        }
        var v6connections = GetAllTCPv6Connections();
        foreach (var connection in v6connections)
        {
            NetworkConnection conn = new();
            int port = 0;
            port += connection.localPort[0] << 8;
            port += connection.localPort[1];

            int rport = 0;
            rport += connection.remotePort[0] << 8;
            rport += connection.remotePort[1];

            var la = connection.localAddr;
            var ra = connection.remoteAddr;

            var localAddr = "";
            var remoteAddr = "";
            for (int i = 0; i < la.Length; i++)
            {
                if (i % 2 == 0)
                {
                    if (i != 0)
                    {
                        localAddr += ":";
                    }
                    if (la[i] == 0x00 && la[i + 1] == 0x00)
                    {
                        i++;
                        continue;
                    }
                }
                byte[] annoyingArrayAssignment = new byte[1] { la[i] };
                localAddr += BitConverter.ToString(annoyingArrayAssignment);

            }
            for (int i = 0; i < ra.Length; i++)
            {
                if (i % 2 == 0)
                {
                    if (i != 0)
                    {
                        remoteAddr += ":";
                    }
                    if (ra[i] == 0x00 && ra[i + 1] == 0x00)
                    {
                        i++;
                        continue;
                    }
                }
                byte[] annoyingArrayAssignment = new byte[1] { ra[i] };
                remoteAddr += BitConverter.ToString(annoyingArrayAssignment);

            }

            conn.LocalIPAddress = localAddr;
            conn.LocalPort = port;
            conn.RemoteIPAddress = remoteAddr;
            conn.RemotePort = rport;
            conn.OwningPID = connection.owningPid;

            connectionsList.Add(conn);
        }
        return connectionsList;
    }
    private static List<BatteryData> GetBatteryData()
    {
        List<BatteryData> BatteryInfo = new List<BatteryData>();
        String path =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()
                .Location); //Directory the .exe has been launched from

        Process cmd = new Process //Generate the XML report we'll be grabbing the data from
        {
            StartInfo =
            {
                FileName = "powercfg",
                WorkingDirectory = path,
                CreateNoWindow = true,
                Arguments = "/batteryreport /xml",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        cmd.Start();

        Stopwatch timer = Stopwatch.StartNew();
        TimeSpan timeout = new TimeSpan().Add(TimeSpan.FromSeconds(60));

        while (timer.Elapsed < timeout)
            if (File.Exists(Path.Combine(path, "battery-report.xml")) &&
                Process.GetProcessesByName("powercfg").Length == 0)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(path, "battery-report.xml"));
                List<JToken> BatteryData =
                    JObject.Parse(JsonConvert.SerializeXmlNode(doc))["BatteryReport"]["Batteries"].Children().Children()
                        .ToList();

                foreach (JToken battery in BatteryData)
                    if (battery.HasValues)
                    {
                        BatteryInfo.Add(
                            new BatteryData
                            {
                                Name = (string)battery["Id"],
                                Manufacturer = (string)battery["Manufacturer"],
                                Chemistry = (string)battery["Chemistry"],
                                Design_Capacity = (string)battery["DesignCapacity"],
                                Full_Charge_Capacity = (string)battery["FullChargeCapacity"],
                                Remaining_Life_Percentage =
                                    string.Concat(
                                        ((float)battery["FullChargeCapacity"] / (float)battery["DesignCapacity"] * 100)
                                        .ToString("0.00"), "%")
                            });
                    }

                File.Delete(Path.Combine(path, "battery-report.xml"));
                break;
            }

        if (timer.Elapsed > timeout)
            Issues.Add("Battery report was not generated before the timeout!");

        timer.Stop();
        cmd.Close();
        return BatteryInfo;
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
            
            if (appName != null) { 

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
}
