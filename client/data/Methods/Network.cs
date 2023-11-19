using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace specify_client.data;

public static partial class Cache
{
    public static async Task MakeNetworkData()
    {
        try
        {
            DebugLog.Region region = DebugLog.Region.Networking;
            await DebugLog.StartRegion(region);
            NetAdapters = Utils.GetWmi("Win32_NetworkAdapterConfiguration",
                "Description, DHCPEnabled, DHCPServer, DNSDomain, DNSDomainSuffixSearchOrder, DNSHostName, "
                + "DNSServerSearchOrder, IPEnabled, IPAddress, IPSubnet, DHCPLeaseObtained, DHCPLeaseExpires, "
                + "DefaultIPGateway, MACAddress, InterfaceIndex");

            NetAdapters2 = Utils.GetWmi("MSFT_NetAdapter",
                "*",
                @"root\standardcimv2");
            IPRoutes = Utils.GetWmi("Win32_IP4RouteTable",
                "Description, Destination, Mask, NextHop, Metric1, InterfaceIndex");

            await DebugLog.LogEventAsync("Networking WMI Information Retrieved.", region);

            GetAdapterProperties();
            CombineAdapterInformation();

            var rssWmi = Utils.GetWmi("MSFT_NetOffloadGlobalSetting", "ReceiveSideScaling", "root\\standardcimv2").FirstOrDefault();
            rssWmi.TryWmiRead("ReceiveSideScaling", out byte? rcvSideScaling);

            // ReceiveSideScaling is false if the WMI value is zero and true if the WMI value is 1, however the data is stored in WMI as a byte, allowing numbers above 1.
            // This statement will avoid errors if the WMI value is not valid.
            ReceiveSideScaling = rcvSideScaling is not null && rcvSideScaling != 0;

            AutoTuningLevelLocal = GetAutoTuningLevels();

            HostsFile = GetHostsFile();
            HostsFileHash = GetHostsFileHash();
            await DebugLog.LogEventAsync("Hosts file retrieved.", region);

            NetworkConnections = GetNetworkConnections();
            await DebugLog.LogEventAsync("NetworkConnections Information retrieved.", region);

            await DebugLog.EndRegion(DebugLog.Region.Networking);
        }
        catch (Exception ex)
        {
            await DebugLog.LogFatalError($"{ex}", DebugLog.Region.Networking);
        }
        NetworkWriteSuccess = true;
    }

    private static List<NetworkConnection> GetNetworkConnections()
    {
        DateTime start = DateTime.Now;
        DebugLog.LogEvent("GetNetworkConnections() Started.", DebugLog.Region.Networking);
        List<NetworkConnection> connectionsList = new();
        var connections = GetAllTCPv4Connections();
        foreach (var connection in connections)
        {
            NetworkConnection conn = new();

            // port numbers are 16-bit numbers stored in two bytes. The bit-shifts here convert the two bytes into the real port number.
            int port = connection.localPort[0] << 8;
            port += connection.localPort[1];

            int rport = connection.remotePort[0] << 8;
            rport += connection.remotePort[1];

            // IPv4 addresses are returned as a 32-bit unsigned integer and must be converted into a byte array to use in the IPAddress constructor.
            var la = connection.localAddr;
            var localAddrByteArray = BitConverter.GetBytes(la);
            var localAddr = new IPAddress(localAddrByteArray);

            var ra = connection.remoteAddr;
            var remoteAddrByteArray = BitConverter.GetBytes(ra);
            var remoteAddr = new IPAddress(remoteAddrByteArray);

            conn.LocalIPAddress = $"{localAddr}";
            conn.LocalPort = port;
            conn.RemoteIPAddress = $"{remoteAddr}";
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

            IPAddress localIpAddress = new(connection.localAddr);
            IPAddress remoteIpAddress = new(connection.remoteAddr);

            var localAddr = localIpAddress.ToString();
            var remoteAddr = remoteIpAddress.ToString();

            conn.LocalIPAddress = localAddr;
            conn.LocalPort = port;
            conn.RemoteIPAddress = remoteAddr;
            conn.RemotePort = rport;
            conn.OwningPID = connection.owningPid;

            connectionsList.Add(conn);
        }
        DebugLog.LogEvent($"GetNetworkConnections() completed. Total Runtime {(DateTime.Now - start).TotalMilliseconds}", DebugLog.Region.Networking);
        return connectionsList;
    }
    private static void GetAdapterProperties()
    {
        var NICs = Utils.GetWmi("Win32_NetworkAdapter");
        foreach (var adapter in NetAdapters)
        {
            var matchingAdapter = GetMatchingAdapter(adapter, NICs);
            if (matchingAdapter.Count == 0)
            {
                // No matching adapter found. This is fine, NetAdapters doesn't contain all of the adapters Win32_NetworkAdapter contains.
                continue;
            }
            if (matchingAdapter.TryWmiRead("Speed", out ulong LinkSpeed))
            {
                if (LinkSpeed == long.MaxValue)
                {
                    // Unconnected adapters report their link speed as the max value of a signed Int64 despite being an unsigned Int64 in WMI.
                    LinkSpeed = 0;
                }
                adapter.Add("LinkSpeed", LinkSpeed);
            }
            if (matchingAdapter.TryWmiRead("PhysicalAdapter", out bool physicalAdapter))
            {
                adapter.Add("PhysicalAdapter", physicalAdapter);
            }
        }
    }
    private static void CombineAdapterInformation()
    {
        foreach (var adapter in NetAdapters)
        {
            try
            {
                var matchingAdapter = GetMatchingAdapter(adapter, NetAdapters2);
                if (matchingAdapter.Count == 0)
                {
                    continue;
                }
                CombineAdapterInformation(adapter, matchingAdapter);
            }
            catch (ArgumentException)
            {
                DebugLog.LogEvent($"Duplicate entries found for {adapter["Description"]} - This should never happen and implicates duplicated Interface indices in the WMI.", DebugLog.Region.Networking, DebugLog.EventType.ERROR);
            }
        }
    }
    private static Dictionary<string, object> GetMatchingAdapter(Dictionary<string, object> adapter1, List<Dictionary<string, object>> adapters2)
    {
        if (!adapter1.TryWmiRead("InterfaceIndex", out uint adapter1Index))
        {
            DebugLog.LogEvent($"Invalid or nonexistent InterfaceIndex in {adapter1["Description"]}", DebugLog.Region.Networking, DebugLog.EventType.ERROR);
            return new Dictionary<string, object>();
        }
        foreach (var adapter2 in adapters2)
        {
            if (!adapter2.TryWmiRead("InterfaceIndex", out uint adapter2Index))
            {
                // If this error is present, it will occur multiple times and clog the debug log. Possible to fix?
                // Should an integrity check on these dictionaries be run prior to iterating through them?
                DebugLog.LogEvent($"Invalid or nonexistent InterfaceIndex in {adapter2["Description"]}", DebugLog.Region.Networking, DebugLog.EventType.ERROR);
                continue;
            }
            if (adapter1Index == adapter2Index)
            {
                return adapter2;
            }
        }
        return new Dictionary<string, object>();
    }
    private static void CombineAdapterInformation(Dictionary<string, object> adapter, Dictionary<string, object> adapter2)
    {

        List<bool> NetAdapter2Integrity = new()
        {
            adapter2.TryWmiRead("FullDuplex", out object FullDuplex),
            adapter2.TryWmiRead("MediaConnectState", out object MediaConnectState),
            adapter2.TryWmiRead("MediaDuplexState", out object MediaDuplexState),
            adapter2.TryWmiRead("MtuSize", out object MtuSize),
            adapter2.TryWmiRead("Name", out object Name),
            adapter2.TryWmiRead("OperationalStatusDownMediaDisconnected", out object OperationalStatusDownedMediaDisconnected),
            adapter2.TryWmiRead("PermanentAddress", out object PermanentAddress),
            adapter2.TryWmiRead("PromiscuousMode", out object PromiscuousMode),
            adapter2.TryWmiRead("State", out object State),
            adapter2.TryWmiRead("InterfaceGuid", out object InterfaceGuid)
        };
        if (NetAdapter2Integrity.Contains(false))
        {
            DebugLog.LogEvent($"{adapter["Description"]} information incomplete. MSFT_NetAdapter missing data.", DebugLog.Region.Networking, DebugLog.EventType.WARNING);
        }
        adapter.Add("FullDuplex", FullDuplex);
        adapter.Add("MediaConnectState", MediaConnectState);
        adapter.Add("MediaDuplexState", MediaDuplexState);
        adapter.Add("MtuSize", MtuSize);
        adapter.Add("Name", Name);
        adapter.Add("OperationalStatusDownMediaDisconnected", OperationalStatusDownedMediaDisconnected);
        adapter.Add("PermanentAddress", PermanentAddress);
        adapter.Add("PromiscuousMode", PromiscuousMode);
        adapter.Add("State", State);
        adapter.Add("DNSIPV6", Utils.GetRegistryValue<string>(Registry.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters\\Interfaces\\" + (string)InterfaceGuid, "NameServer"));
        adapter.Add("DNSIsStatic", Utils.GetRegistryValue<string>(Registry.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + (string)InterfaceGuid, "NameServer") != "");

    }
    private static Dictionary<string, string> GetAutoTuningLevels()
    {
        var autotuningLevels = new Dictionary<string, string>();
        var autotuningLevelsWmi = Utils.GetWmi("MSFT_NetTCPSetting", "AutoTuningLevelLocal, PolicyRuleName", "root\\standardcimv2");
        foreach(var levelDict in autotuningLevelsWmi)
        {
            string policyName = (string)levelDict["PolicyRuleName"];
            if(!levelDict.TryWmiRead("AutoTuningLevelLocal", out byte level))
            {
                autotuningLevels.Add(policyName, "");
                continue;
            }
            autotuningLevels.Add(policyName, ((Utils.AutoTuningLevels)level).ToString());
        }
        return autotuningLevels;
    }
    private static List<Interop.MIB_TCPROW_OWNER_PID> GetAllTCPv4Connections()
    {
        return GetTCPConnections<Interop.MIB_TCPROW_OWNER_PID, Interop.MIB_TCPTABLE_OWNER_PID>(AF_INET);
    }

    private static List<Interop.MIB_TCP6ROW_OWNER_PID> GetAllTCPv6Connections()
    {
        return GetTCPConnections<Interop.MIB_TCP6ROW_OWNER_PID, Interop.MIB_TCP6TABLE_OWNER_PID>(AF_INET6);
    }

    private static List<IPR> GetTCPConnections<IPR, IPT>(int ipVersion)
    {
        IPR[] tableRows = null;
        int buffSize = 0;
        var dwNumEntriesField = typeof(IPT).GetField("dwNumEntries");

        Interop.GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, ipVersion, Interop.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
        IntPtr tcpTablePtr = Marshal.AllocHGlobal(buffSize);

        try
        {
            uint ret = Interop.GetExtendedTcpTable(tcpTablePtr, ref buffSize, true, ipVersion, Interop.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
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
        catch (Exception e)
        {
            DebugLog.LogEvent("Unexpected exception in GetTCPConnections", DebugLog.Region.Networking, DebugLog.EventType.ERROR);
            DebugLog.LogEvent($"{e}", DebugLog.Region.Networking, DebugLog.EventType.INFORMATION);
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }
        return tableRows != null ? tableRows.ToList() : new List<IPR>();
    }
    private static string GetHostsFile()
    {
        StringBuilder hostsFile = new();
        try
        {
            foreach (var str in File.ReadAllLines(@"C:\Windows\System32\drivers\etc\hosts"))
            {
                hostsFile.Append($"{str}\n");
            }
        }
        catch (FileNotFoundException)
        {
            DebugLog.LogEvent("Hosts file not found.", DebugLog.Region.Networking, DebugLog.EventType.ERROR);
            return "";
        }
        return hostsFile.ToString();
    }
    public static string GetHostsFileHash()
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        try
        {
            using var stream = File.OpenRead(@"C:\Windows\System32\drivers\etc\hosts");
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        } 
        catch (FileNotFoundException)
        {
            // If this catch happens, the error has already been reported in GetHostsFile() and does not need to be reported again.
            return "";
        }
        
    }
}