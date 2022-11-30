// everything that handles actual data collection to be passed over to the monolith
using System;
using System.Collections;
using System.Management;
using Microsoft.Win32.TaskScheduler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;
using System.IO;
//using System.Threading.Tasks;

namespace specify_client
{
    public class Data
    {
        /**
         * <summary>
         * Gets the WMI object (with GetWmiObj), and converts it to a dictionary.
         * </summary>
         * <seealso cref="GetWmiObj"/>
         */
        public static List<Dictionary<string, object>> GetWmi(string cls, string selected = "*", string ns = @"root\cimv2")
        {
            var collection = GetWmiObj(cls, selected, ns);
            var res = new List<Dictionary<string, object>>();

            foreach (var i in collection)
            {
                var tempD = new Dictionary<string, object>();
                foreach (var j in i.Properties)
                {
                    tempD[j.Name] = j.Value;
                }
                
                res.Add(tempD);
            }

            return res;
        }

        /**
         * <summary>
         * Gets the WMI Object for the specified query. Try to use GetWmi when possible.
         * </summary>
         * <remarks>
         * Microsoft recommends using the CIM libraries (Microsoft.Management.Infrastructure).
         * However, some classes can't be called in CIM and only in WMI (e.g. Win32_PhysicalMemory).
         * </remarks>
         * <seealso cref="GetWmi"/>
         */
        public static ManagementObjectCollection GetWmiObj(string cls, string selected = "*", string ns = @"root\cimv2")
        {
            var scope = new ManagementScope(ns);
            scope.Connect();

            var query = new ObjectQuery($"SELECT {selected} FROM {cls}");
            var collection = new ManagementObjectSearcher(scope, query).Get();
            return collection;
        }

        /**
         * <summary>
         * Gets tasks from Task Scheduler that satisfy all of the following conditions:
         * <list type="bullet">
         *  <item>Author isn't Microsoft</item>
         *  <item>State is Ready or Running</item>
         *  <item>Triggered at boot or login</item>
         * </list>
         * </summary>
         */
        public static List<Task> GetTsStartupTasks()
        {
            var ts = new TaskService();
            return EnumTsTasks(ts.RootFolder);
        }

        private static List<Task> EnumTsTasks(TaskFolder fld)
        {
            var res = new List<Task>();
            foreach (var task in fld.Tasks)
            {
                if (
                    !string.IsNullOrEmpty(task.Definition.RegistrationInfo.Author) &&
                    task.Definition.RegistrationInfo.Author.StartsWith("Microsof")
                    ) continue;

                if (task.State != TaskState.Ready || task.State != TaskState.Running) continue;
                
                var triggers = task.Definition.Triggers;
                var triggersFlag = true;
                foreach (var trigger in triggers)
                {
                    if (trigger.TriggerType == TaskTriggerType.Logon || trigger.TriggerType == TaskTriggerType.Boot)
                        triggersFlag = false;
                }

                if (triggersFlag) continue;

                res.Add(task);
            }
            
            foreach (var sfld in fld.SubFolders)
                res.AddRange(EnumTsTasks(sfld));

            return res;
        }

        /**
         * Convert a CIM date (what would be gotten from WMI) into an ISO date
         * See https://learn.microsoft.com/en-us/windows/win32/wmisdk/cim-datetime
         */
        public static string CimToIsoDate(string cim)
        {
            return DateTimeToIsoDate(ManagementDateTimeConverter.ToDateTime(cim));
        }

        public static string DateTimeToIsoDate(DateTime d)
        {
            return d.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }
        
        public static T GetRegistryValue<T>(RegistryKey regKey, string path, string name, T def = default)
        {
            var key = regKey.OpenSubKey(path);
            if (key == null) return def;
            var value = key.GetValue(name);
            return (T)value;
        }
    }

    /**
     * Cache of values so they don't have to be called multiple times
     */
    public static class DataCache
    {
        public static List<string> Issues { get; set; }
        public static Dictionary<string, object> Os { get; private set; }
        public static Dictionary<string, object> Cs { get; private set; }
        public static IDictionary SystemVariables { get; private set; }
        public static IDictionary UserVariables { get; private set; }
        public static List<OutputProcess> RunningProcesses { get; private set; }
        public static List<Dictionary<string, object>> Services { get; private set; }
        public static List<Dictionary<string, object>> InstalledApps { get; private set; }
        public static List<Dictionary<string, object>> InstalledHotfixes { get; private set; }
        public static Dictionary<string, DateTime?> ScheduledTasks { get; private set; }
        public static List<string> AvList { get; private set; }
        public static List<string> FwList { get; private set; }
        public static string HostsFile { get; private set;  }
        public static bool? UacEnabled { get; private set; }
        public static int? UacLevel { get; private set; }
        public static List<Dictionary<string, object>> NetAdapters { get; private set; }
        public static List<Dictionary<string, object>> IPRoutes { get; private set; }

        public static string Username => Environment.UserName;
        // all the hardware stuff
        //each item in the list is a stick of ram
        public static List<RamStick> Ram {get; private set;}
        public static List<DiskDrive> Disks { get; private set; }
        public static Dictionary<string, object> Cpu {get; private set;}
        public static List<Dictionary<string, object>> Gpu {get; private set;}
        public static Dictionary<string, object> Motherboard {get; private set;}
        public static Dictionary<string, object> Tpm { get; private set; }
        public static List<Dictionary<string, object>> Drivers { get; private set; }
        public static List<Dictionary<string, object>> Devices { get; private set; }
        public static bool? SecureBootEnabled { get; private set; }
        private static readonly List<string> SystemProcesses = new List<string>()
        {
            "Memory Compression",
            "Registry",
            "System",
            "Idle"
        };

        // DllImports to P/Invoke process information queries. The typical C# process call fails on certain system processes.
        [Flags]
        private enum ProcessAccessFlags : uint
        {
            QueryLimitedInformation = 0x00001000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName(
              [In] IntPtr hProcess,
              [In] int dwFlags,
              [Out] StringBuilder lpExeName,
              ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
         ProcessAccessFlags processAccess,
         bool bInheritHandle,
         int processId);

        public static void MakeMainData()
        {
            Os = Data.GetWmi("Win32_OperatingSystem").First();
            Cs = Data.GetWmi("Win32_ComputerSystem").First();
        }

        public static void DummyTimer()
        {
            Thread.Sleep(5000);
        }

        public static void MakeSystemData()
        {
            
            SystemVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
            UserVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            Services = Data.GetWmi("Win32_Service", "Name, Caption, PathName, StartMode, State");
            InstalledApps = Data.GetWmi("Win32_Product", "Name, Version");
            InstalledHotfixes = Data.GetWmi("Win32_QuickFixEngineering", "Description,HotFixID,InstalledOn");
            ScheduledTasks = GetScheduledTasks();
            RunningProcesses = new List<OutputProcess>();
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
                    int capacity = 2000;

                    StringBuilder sb = new StringBuilder(capacity);
                    IntPtr ptr = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, rawProcess.Id);

                    if (!QueryFullProcessImageName(ptr, 0, sb, ref capacity))
                    {
                        if (!SystemProcesses.Contains(rawProcess.ProcessName))
                        {
                            exePath = "null - Not found";
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
        private static Dictionary<string, DateTime?> GetScheduledTasks()
        {
            // This starts a cmd process and pipes the output of schtasks /query back to Specified.
            // I would like a way to do this that doesn't ask to start another process. Defender won't appreciate this.
            string command = "schtasks /query";
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c " + command);
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            Process proc = new Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            string tasklist = proc.StandardOutput.ReadToEnd();

            // Trim the output of the previous command into something more workable.
            var splitTaskList = tasklist.Split('\n');
            var trimmedTaskList = TrimTaskList(splitTaskList);

            Dictionary<string, DateTime?> returnList = new Dictionary<string, DateTime?>();

            // Iterate the trimmed task list and convert each task into serializable data.
            foreach(var task in trimmedTaskList)
            {
                // There is a method to ignore empty strings, however .NET 4.6 has an overload selection bug that will not allow the app to compile when using that method.
                var splitTask = task.Split(' ');

                // Inefficient method to remove empty strings. The resulting list is:
                // [0]: Task name
                // [1]: Scheduled DateTime
                // [2]: Status (Ready/Disabled)
                // [3]: '\r'
                List<string> SplitTaskAsList = new List<string>();
                for(int i = 0; i < splitTask.Length; i++)
                {
                    var segment = splitTask[i];
                    if (segment.Count() == 0)
                    {
                        continue;
                    }
                    // If the list is empty, you're working on the task name. Combine strings to get the full task name.
                    if (SplitTaskAsList.Count == 0)
                    {
                        for(int j = i+1; j < splitTask.Length; j++)
                        {
                            // An empty string marks the end of a name.
                            if (splitTask[j].Count() == 0)
                            {
                                break;
                            }

                            // A string labeled "N/A" is an empty datetime field.
                            if (splitTask[j].StartsWith(@"N/A"))
                            {
                                break;
                            }

                            // If the string is a valid date, it is no longer part of the task name.
                            if (DateTime.TryParse(splitTask[j], out DateTime discard))
                            {
                                break;
                            }
                            
                            segment += $" {splitTask[j]}";
                            i++;
                        }
                    }
                    // If the list contains one element, you're working on the scheduled datetime. segment (splitTask[i]) is the date, splitTask[i+1] is the time.
                    if(SplitTaskAsList.Count == 1)
                    {
                        segment += $" {splitTask[i+1]}";
                        i++;
                    }
                    SplitTaskAsList.Add(segment);
                }

                DateTime? ScheduledTime;
                // I can't do TryParse(string, out Datetime?) ?! That seems absurd.
                DateTime RidiculousVariable;

                if(!DateTime.TryParse(SplitTaskAsList[1], out RidiculousVariable))
                {
                    // The task is not scheduled.
                    ScheduledTime = null;
                }
                else
                {
                    ScheduledTime = RidiculousVariable;
                }


                try
                {
                    returnList.Add(SplitTaskAsList[0], ScheduledTime);
                }
                catch (ArgumentException)
                {
                    // If there is already a task of the same name in the dictionary, ignore the new task. This is probably unwise.
                    continue;
                }
            }
            return returnList;
        }
        private static List<string> TrimTaskList(string[] taskList)
        {
            List<string> TrimList = new List<string>();

            foreach (string line in taskList)
            {
                // Ignore empty and formatting strings.
                if (line.Count() == 0)
                {
                    continue;
                }
                if (!char.IsLetterOrDigit(line[0]))
                {
                    continue;
                }
                if (line.StartsWith("Folder:"))
                {
                    continue;
                }
                if (line.StartsWith("==="))
                {
                    continue;
                }
                if (line.StartsWith("TaskName"))
                {
                    continue;
                }
                // Remove errored task strings.
                if (line.StartsWith("INFO:"))
                {
                    // TODO: This needs some sort of error message, maybe an issue.
                    continue;
                }
                TrimList.Add(line);
            }
            return TrimList;
        }
        public static void MakeHardwareData()
        {
            Cpu = Data.GetWmi("Win32_Processor",
                "CurrentClockSpeed, Manufacturer, Name, SocketDesignation").First();
            Gpu = Data.GetWmi("Win32_VideoController",
                "Description, AdapterRam, CurrentHorizontalResolution, CurrentVerticalResolution, "
                + "CurrentRefreshRate, CurrentBitsPerPixel");
            Motherboard = Data.GetWmi("Win32_BaseBoard", "Manufacturer, Product, SerialNumber").First();
            Drivers = Data.GetWmi("Win32_PnpSignedDriver", "FriendlyName,Manufacturer,DeviceID,DeviceName,DriverVersion");
            Devices = Data.GetWmi("Win32_PnpEntity", "DeviceID,Name,Description,Status");
            Ram = GetSMBiosMemoryInfo();
            Disks = GetDiskDriveData();
        }

        public static void MakeSecurityData()
        {
            AvList = Data.GetWmi("AntivirusProduct", "displayName", @"root\SecurityCenter2")
                .Select(x => (string)x["displayName"]).ToList();
            FwList = Data.GetWmi("FirewallProduct", "displayName", @"root\SecurityCenter2")
                .Select(x => (string)x["displayName"]).ToList();
            
            var enableLua = Data.GetRegistryValue<int?>(Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA");
            if (enableLua == null) Issues.Add($"Security data: could not get EnableLUA value");
            else UacEnabled = enableLua == 1;

            if (Environment.GetEnvironmentVariable("firmware_type").Equals("UEFI"))
            {
                var secBootEnabled = Data.GetRegistryValue<int?>(
                    Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SecureBoot\State",
                    "UEFISecureBootEnabled");

                if (secBootEnabled == null) Issues.Add($"Security data: could not get UEFISecureBootEnabled value");
                else SecureBootEnabled = secBootEnabled == 1;
            }
            
            try 
            { 
                Tpm = Data.GetWmi("Win32_Tpm","*", @"Root\CIMV2\Security\MicrosoftTpm").First();
                Tpm["IsPresent"] = true;
            }
            catch (InvalidOperationException)
            {
                // No TPM
                Tpm = new Dictionary<string, object>(){{ "IsPresent", false }};
            }
            catch (ManagementException)
            {
                Tpm = null;
                Issues.Add("Security Data: could not get TPM. This is probably because specify was not run as administrator.");
            }

            UacLevel = Data.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                "ConsentPromptBehaviorAdmin");
        }
        public static void MakeNetworkData()
        {
            NetAdapters = Data.GetWmi("Win32_NetworkAdapterConfiguration", 
                "Description, DHCPEnabled, DHCPServer, DNSDomain, DNSDomainSuffixSearchOrder, DNSHostName, "
                    + "DNSServerSearchOrder, IPEnabled, IPAddress, IPSubnet, DHCPLeaseObtained, DHCPLeaseExpires, "
                    + "DefaultIPGateway, MACAddress, InterfaceIndex");
            IPRoutes = Data.GetWmi("Win32_IP4RouteTable", 
                "Description, Destination, Mask, NextHop, Metric1, InterfaceIndex");
            HostsFile = System.IO.File.ReadAllText(@"C:\Windows\system32\drivers\etc\hosts");

            // Uncomment the block below to run a traceroute to Google's DNS
            /*var NetStats = await GetNetworkRoutes("8.8.8.8", 1000);
            for (int i = 0; i < NetStats.Address.Count; i++)
            {
                Console.WriteLine($"{i}: {NetStats.Address[i]} --- Lat: {NetStats.AverageLatency[i]} --- PL: {NetStats.PacketLoss[i]}");
            }*/
        }
        private static async System.Threading.Tasks.Task<NetworkRoute> GetNetworkRoutes(string ipAddress, int pingCount = 100, int timeout = 10000,  int bufferSize = 100)
        {
            var AddressList = GetTraceroute(ipAddress, timeout, 30, bufferSize);
            var networkRoute = new NetworkRoute();

            foreach(var address in AddressList)
            {
                networkRoute.Address.Add(address.ToString());
                var hostStats = await GetHostStats(ipAddress, timeout, pingCount);
                networkRoute.AverageLatency.Add(hostStats.Key);
                networkRoute.PacketLoss.Add(hostStats.Value);
            }

            return networkRoute;
        }
        // I don't like this returning a KeyValuePair, but .NET 4.6 does not innately support tuples.
        private static async System.Threading.Tasks.Task<KeyValuePair<int,double>> GetHostStats(string ipAddress, int timeout = 10000, int pingCount = 100)
        {
            Ping pinger = new Ping();
            PingOptions pingOptions = new PingOptions();

            string data = "meaninglessdatawithalotofletters"; // 32 letters in total.
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            int failedPings = 0;
            int errors = 0;
            int latencySum = 0;
            List<System.Threading.Tasks.Task<int>> statTasks = new List<System.Threading.Tasks.Task<int>>();

            for(int i = 0; i < pingCount; i++)
            {
                System.Threading.Tasks.Task<int> task = System.Threading.Tasks.Task.Run(() => GetLatency(ipAddress, timeout, buffer, pingOptions));
                statTasks.Add(task);
            }
            await System.Threading.Tasks.Task.WhenAll(statTasks);
            foreach(var task in statTasks)
            {
                if(task.Result == -1)
                {
                    failedPings++;
                }
                else if(task.Result == -2)
                {
                    errors++;
                }
                else
                {
                    latencySum += task.Result;
                }
            }
            int averageLatency = latencySum / pingCount;
            double packetLoss = (double)failedPings / (double)pingCount;
            if (errors > 0)
            {
                Console.WriteLine($"{ipAddress} - ERRORS: {errors}");
            }
            return new KeyValuePair<int, double>(averageLatency, packetLoss);
        }
        private static async System.Threading.Tasks.Task<int> GetLatency(string ipAddress, int timeout, byte[] buffer,PingOptions pingOptions)
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
            if(maxTTL > 30)
            {
                maxTTL = 30;
            }

            byte[] buffer = new byte[bufferSize];
            new Random().NextBytes(buffer);

            using (Ping pingTool = new Ping())
            {
                foreach(int i in Enumerable.Range(1, maxTTL))
                {
                    PingOptions pingOptions = new PingOptions(i, true);
                    PingReply reply = pingTool.Send(ipAddress, timeout, buffer, pingOptions);

                    if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
                    {
                        yield return reply.Address;
                    }
                    if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
                    {
                        break;
                    }
                }
            }
        }
        private static List<DiskDrive> GetDiskDriveData()
        {
            List<DiskDrive> drives = new List<DiskDrive>();

            var DriveWmiInfo = Data.GetWmiObj("Win32_DiskDrive");

            // This assumes the WMI info reports disks in order by drive number. I'm not certain this is a safe assumption.
            int diskNumber = 0;
            foreach (ManagementObject driveWmi in DriveWmiInfo)
            {
                DiskDrive drive = new DiskDrive();

                drive.DeviceName = ((string)driveWmi["Model"]).Trim();
                drive.SerialNumber = ((string)driveWmi["SerialNumber"]).Trim();
                drive.DiskNumber = diskNumber;
                drive.DiskCapacity = (UInt64)driveWmi["Size"];
                drive.BlockSize = (UInt32)driveWmi["BytesPerSector"];
                drive.InstanceId = (string)driveWmi["PNPDeviceID"];

                drive.Partitions = new List<Partition>();
                
                diskNumber++;
                drives.Add(drive);
            }

            var PartitionWmiInfo = Data.GetWmiObj("Win32_DiskPartition");
            foreach (ManagementObject partitionWmi in PartitionWmiInfo)
            {
                Partition partition = new Partition()
                {
                    PartitionCapacity = (UInt64)partitionWmi["Size"],
                };
                UInt32 diskIndex = (UInt32)partitionWmi["DiskIndex"];
                
                drives[(int)diskIndex].Partitions.Add(partition);
            }
            try
            {
                var queryCollection = Data.GetWmiObj("MSStorageDriver_FailurePredictData", "*", "\\\\.\\root\\wmi");
                foreach (ManagementObject m in queryCollection)
                {

                    // The following lines up to the attribute list creationlink smart data to its corresponding drive.
                    // It makes the assumption that the PNPDeviceID in Wmi32_DiskDrive has a matching identification code to MSStorageDriver_FailurePredictData's InstanceID, and that these identification codes are unique.
                    // This is not a safe assumption, testing will be required.
                    string InstanceId = (string)m["InstanceName"];
                    InstanceId = InstanceId.Substring(0, InstanceId.Length - 2);
                    var splitID = InstanceId.Split('\\');
                    InstanceId = splitID[splitID.Count() - 1];

                    int driveIndex = -1;
                    for(int i = 0; i < drives.Count; i++)
                    {
                        var drive = drives[i];
                        if(drive.InstanceId.ToLower().Contains(InstanceId.ToLower()))
                        {
                            driveIndex = i;
                            break;
                        }
                    }

                    if(driveIndex == -1)
                    {
                        Issues.Add($"Smart Data found for {InstanceId} with no matching drive. This is a Specify error");
                        break;
                    }

                    List<SmartAttribute> diskAttributes = new List<SmartAttribute>();

                    byte[] vs = (byte[])m["VendorSpecific"];
                    // Every 12th byte starting at byte index 2 is a smart identifier.
                    for (int i = 2; i < vs.Length; i += 12)
                    {
                        byte[] c = new byte[12];
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
                Issues.Add("Error retrieving SMART Data." + e.Message);
            }
            var partitionInfo = Data.GetWmiObj("Win32_Volume");
            foreach (var partition in partitionInfo)
            {
                // Check if partition drive size is identical to exactly one partition drive size in the list of disks. If it is, add win32_volume data to it.
                // If it is not, create an issue for the failed link.
                UInt64 partitionSize = 0;
                UInt64 blockSize = 0;
                try
                {
                    partitionSize = (UInt64)partition["Capacity"];
                    blockSize = (UInt64)partition["BlockSize"];
                }
                catch (NullReferenceException)
                {
                    Issues.Add("Failure to parse partition information - No capacity found. This is likely a virtual or unallocated drive.");
                    continue;
                }

                // Drive and Partition indices.
                int dIndex = -1;
                int pIndex = -1;

                // Indicators; If found and unique, store partition information with the drive under dIndex/pIndex.
                // Otherwise, store into non-specific partition list.
                bool found = false;
                bool unique = true;
                for (int di = 0; di < drives.Count(); di++)
                {
                    for (int pi = 0; pi < drives[di].Partitions.Count(); pi++)
                    {
                        var fileSystem = (string)partition["FileSystem"];
                        if (fileSystem.ToLower().Equals("ntfs"))
                        {
                            if (partitionSize == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize + blockSize == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize - blockSize == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize + 2048 == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize - 2048 == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize + 1024 == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize - 1024 == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize + 4096 == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize - 4096 == drives[di].Partitions[pi].PartitionCapacity)
                            {
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
                        else if (fileSystem.ToLower().Equals("fat32") || fileSystem.ToLower().Equals("exfat32"))
                        {
                            if (partitionSize == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize + (2048 * 2048) == drives[di].Partitions[pi].PartitionCapacity ||
                                partitionSize - (2048 * 2048) == drives[di].Partitions[pi].PartitionCapacity)
                            {
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
                    }
                    // If it is not unique, no drive or partition index is valid. Stop checking.
                    if (!unique)
                    {
                        dIndex = -1;
                        pIndex = -1;
                        break;
                    }
                }
                if (found && unique)
                {
                    // These should never be -1, however they seem to happen occasionally.
                    // Prevent the exception by continuing the loop.
                    if (dIndex == -1 || pIndex == -1)
                    {
                        continue;
                    }
                    var matchingPartition = drives[dIndex].Partitions[pIndex];
                    var DriveLetter = partition["DriveLetter"];
                    if (DriveLetter != null)
                    {
                        matchingPartition.PartitionLabel = (string)DriveLetter;
                    }
                    matchingPartition.PartitionFree = (UInt64)partition["FreeSpace"];
                    var FileSystem = partition["FileSystem"];
                    if (FileSystem != null)
                    {
                        matchingPartition.Filesystem = (string)FileSystem;
                    }
                }
                else
                {
                    var DriveLetter = partition["DriveLetter"];
                    string letter = "";
                    if (DriveLetter != null)
                    {
                    }
                    Issues.Add($"Partition link could not be established for {partitionSize} B partition - Drive Label: {letter}");
                }
            }
            return drives;
        }
        private static SmartAttribute GetAttribute(byte[] data)
        {
            // Smart data is fed backwards, with byte 10 being the first byte for the attribute and byte 5 being the last.
            byte[] values = new byte[6]
            {
                data[10], data[9], data[8], data[7], data[6], data[5]
            };

            SmartAttribute attribute = new SmartAttribute()
            {
                Id = data[0],
                Name = GetAttributeName(data[0]),
            };
            string rawValue = BitConverter.ToString(values);

            rawValue = rawValue.Replace("-", string.Empty);
            attribute.RawValue = rawValue;
            return attribute;
        }
        private static string GetAttributeName(byte id)
        {
            switch (id)
            {
                case 0x1:
                    return "Read Error Rate";
                case 0x2:
                    return "Throughput Performance";
                case 0x3:
                    return "Spin-Up Time";
                case 0x4:
                    return "Start/Stop Count";
                case 0x5:
                    return "Reallocated Sectors Count(!)";
                case 0x6:
                    return "Read Channel Margin";
                case 0x7:
                    return "Seek Error Rate";
                case 0x8:
                    return "Seek Time Performance";
                case 0x9:
                    return "Power-On Hours";
                case 0xA:
                    return "Spin Retry Count(!)";
                case 0xB:
                    return "Calibration Retry Count";
                case 0xC:
                    return "Power Cycle Count";
                case 0xD:
                    return "Soft Read Error Rate";
                case 0x16:
                    return "Current Helium Level";
                case 0x17:
                    return "Helium Condition Lower";
                case 0x18:
                    return "Helium Condition Upper";
                case 0xAA:
                    return "Available Reserved Space";
                case 0xAB:
                    return "SSD Program Fail Count";
                case 0xAC:
                    return "SSD Erase Fail Count";
                case 0xAD:
                    return "SSD Wear Leveling Count";
                case 0xAE:
                    return "Unexpected Power Loss Count";
                case 0xAF:
                    return "Power Loss Protection Failure";
                case 0xB0:
                    return "Erase Fail Count";
                case 0xB1:
                    return "Wear Range Delta";
                case 0xB2:
                    return "Used Reserved Block Count";
                case 0xB3:
                    return "Used Reserved Block Count Total";
                case 0xB4:
                    return "Unused Reserved Block Count Total";
                case 0xB5:
                    return "Vendor Specific"; // Program Fail Count Total or Non-4K Aligned Access Count
                case 0xB6:
                    return "Erase Fail Count";
                case 0xB7:
                    return "Vendor Specific (WD or Seagate)"; //SATA Downshift Error Count or Runtime Bad Block. WD or Seagate respectively.
                case 0xB8:
                    return "End-to-end Error Count(!)";
                case 0xB9:
                    return "Head Stability";
                case 0xBA:
                    return "Induced Op-Vibration Detection";
                case 0xBB:
                    return "Reported Uncorrectable Errors(!)";
                case 0xBC:
                    return "Command Timeout(!)";
                case 0xBD:
                    return "High Fly Writes(!)";
                case 0xBE:
                    return "Airflow Temperature";
                case 0xBF:
                    return "G-Sense Error Rate";
                case 0xC0:
                    return "Unsafe Shutdown Count";
                case 0xC1:
                    return "Load Cycle Count";
                case 0xC2:
                    return "Temperature";
                case 0xC3:
                    return "Hardware ECC Recovered";
                case 0xC4:
                    return "Reallocation Event Count(!)";
                case 0xC5:
                    return "Current Pending Sector Count(!)";
                case 0xC6:
                    return "Uncorrectable Sector Count(!)";
                case 0xC7:
                    return "UltraDMA CRC Error Count";
                case 0xC8:
                    return "Multi-Zone Error Rate(!)(Unless Fujitsu)";
                case 0xC9:
                    return "Soft Read Error Rate(!)";
                case 0xCA:
                    return "Data Address Mark Errors";
                case 0xCB:
                    return "Run Out Cancel";
                case 0xCC:
                    return "Soft ECC Correction";
                case 0xCD:
                    return "Thermal Asperity Rate";
                case 0xCE:
                    return "Flying Height";
                case 0xCF:
                    return "Spin High Current";
                case 0xD0:
                    return "Spin Buzz";
                case 0xD1:
                    return "Offline Seek Performance";
                case 0xD2:
                    return "Vibration During Write";
                case 0xD3:
                    return "Vibration During Write";
                case 0xD4:
                    return "Shock During Write";
                case 0xDC:
                    return "Disk Shift";
                case 0xDD:
                    return "G-Sense Error Rate";
                case 0xDE:
                    return "Loaded Hours";
                case 0xDF:
                    return "Load/Unload Retry Count";
                case 0xE0:
                    return "Load Friction";
                case 0xE1:
                    return "Load/Unload Cycle Count";
                case 0xE2:
                    return "Load 'In'-time";
                case 0xE3:
                    return "Torque Amplification Count";
                case 0xE4:
                    return "Power-Off Retract Cycle";
                case 0xE6:
                    return "GMR Head Amplitude / Drive Life Protection Status"; // HDDs / SSDs respectively.
                case 0xE7:
                    return "SSD Life Left / HDD Temperature";
                case 0xE8:
                    return "Vendor Specific"; // Endurance Remaining or Available Reserved Space.
                case 0xE9:
                    return "Media Wearout Indicator";
                case 0xEA:
                    return "Average and Maximum Erase Count";
                case 0xEB:
                    return "Good Block and Free Block Count";
                case 0xF0:
                    return "Head Flying Hours (Unless Fujitsu)";
                case 0xF1:
                    return "Total LBAs Written";
                case 0xF2:
                    return "Total LBAs Read";
                case 0xF3:
                    return "Total LBAs Written Expanded";
                case 0xF4:
                    return "Total LBAs Read Expanded";
                case 0xF9:
                    return "NAND Writes (# of GiB)";
                case 0xFA:
                    return "Read Error Retry Rate";
                case 0xFB:
                    return "Minimum Spares Remaining";
                case 0xFC:
                    return "Newly Added Bad Flash Block";
                case 0xFE:
                    return "Free Fall Protection";
                default:
                    return "Vendor Specific";
            };
        }
        private static List<RamStick> GetSMBiosMemoryInfo()
        {
            // Made a new GetWmi function because I'm not knowledgeable enough in how this works to translate the Dictionary object into what I need for this.
            var SMBiosObj = Data.GetWmiObj("MSSMBios_RawSMBiosTables", "*", "root\\WMI");

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

                List<string> smbStringsList = new List<string>();

                if (offset < SMBios.Length && SMBios[offset] == 0)
                    offset++;

                // Iterate the byte array to build a list of SMBios structures.
                while (offset < SMBios.Length && SMBios[offset] != 0)
                {
                    System.Text.StringBuilder smbDataString = new System.Text.StringBuilder();
                    while (offset < SMBios.Length && SMBios[offset] != 0)
                    {
                        smbDataString.Append((char)SMBios[offset]);
                        offset++;
                    }
                    offset++;
                    smbStringsList.Add(smbDataString.ToString());
                    // Console.WriteLine(smbDataString);
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
    }
    public class NetworkRoute
    {
        public List<string> Address = new List<string>();
        public List<int> AverageLatency = new List<int>();
        public List<double> PacketLoss = new List<double>();
    }
    public class OutputProcess
    {
        public string ProcessName;
        public string ExePath;
        public int Id;
        public long WorkingSet;
        public double CpuPercent;
    }

    public class RamStick
    {
        public string DeviceLocation;
        public string BankLocator;
        public string Manufacturer;
        public string SerialNumber;
        public string PartNumber;
        /** MHz */
        public int? ConfiguredSpeed;

        /** MiB */
        public int? Capacity;
    }
    public class DiskDrive
    {
        public string DeviceName;
        public string SerialNumber;
        public int DiskNumber;
        public UInt64 DiskCapacity;
        public string DiskFree;
        public UInt32 BlockSize;
        public List<Partition> Partitions;
        public List<SmartAttribute> SmartData;
        [NonSerialized()] public string InstanceId; // Only used to link SmartData, do not serialize. Unless you really want to.
    }
    public class Partition
    {
        public UInt64 PartitionCapacity;
        public UInt64 PartitionFree;
        public string PartitionLabel;
        public string Filesystem;
    }
    public class SmartAttribute
    {
        public byte Id;
        public string Name;
        public string RawValue;
    }
}
