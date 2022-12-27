using System;
using System.Text;

namespace specify_client;

using System.Runtime.InteropServices;

public static class Interop
{
    [Flags]
    internal enum ProcessAccessFlags : uint
    {
        QueryLimitedInformation = 0x00001000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool QueryFullProcessImageName(
        [In] IntPtr hProcess,
        [In] int dwFlags,
        [Out] StringBuilder lpExeName,
        ref int lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr OpenProcess(
        ProcessAccessFlags processAccess,
        bool bInheritHandle,
        int processId);

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public uint remoteAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] remotePort;
        public uint owningPid;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 1)]
        public MIB_TCPROW_OWNER_PID[] table;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCP6ROW_OWNER_PID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] localAddr;
        public uint localScopeId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] remoteAddr;
        public uint remoteScopeId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] remotePort;
        public uint state;
        public uint owningPid;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCP6TABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 1)]
        public MIB_TCP6ROW_OWNER_PID[] table;
    }
    internal enum TCP_TABLE_CLASS
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }
    [DllImport("iphlpapi.dll", SetLastError = true)]
    internal static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, uint reserved = 0);

    [DllImport("kernel32", SetLastError = true)] // this function is used to get the pointer on the process heap required by AllocateAndGetTcpExTableFromStack
    internal static extern IntPtr GetProcessHeap();
}
