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

    public const int ERROR_SUCCESS = 0;

    public enum QUERY_DEVICE_CONFIG_FLAGS : uint
    {
        QDC_ALL_PATHS = 0x00000001,
        QDC_ONLY_ACTIVE_PATHS = 0x00000002,
        QDC_DATABASE_CURRENT = 0x00000004
    }

    public enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : uint
    {
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_OTHER = 0xFFFFFFFF,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 = 0,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SVIDEO = 1,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPOSITE_VIDEO = 2,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPONENT_VIDEO = 3,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI = 4,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI = 5,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_LVDS = 6,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_D_JPN = 8,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDI = 9,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL = 10,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED = 11,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EXTERNAL = 12,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EMBEDDED = 13,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDTVDONGLE = 14,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_MIRACAST = 15,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL = 0x80000000,
        DISPLAYCONFIG_OUTPUT_TECHNOLOGY_FORCE_UINT32 = 0xFFFFFFFF
    }

    public enum DISPLAYCONFIG_SCANLINE_ORDERING : uint
    {
        DISPLAYCONFIG_SCANLINE_ORDERING_UNSPECIFIED = 0,
        DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE = 1,
        DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED = 2,
        DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED_UPPERFIELDFIRST = DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED,
        DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED_LOWERFIELDFIRST = 3,
        DISPLAYCONFIG_SCANLINE_ORDERING_FORCE_UINT32 = 0xFFFFFFFF
    }

    public enum DISPLAYCONFIG_ROTATION : uint
    {
        DISPLAYCONFIG_ROTATION_IDENTITY = 1,
        DISPLAYCONFIG_ROTATION_ROTATE90 = 2,
        DISPLAYCONFIG_ROTATION_ROTATE180 = 3,
        DISPLAYCONFIG_ROTATION_ROTATE270 = 4,
        DISPLAYCONFIG_ROTATION_FORCE_UINT32 = 0xFFFFFFFF
    }

    public enum DISPLAYCONFIG_SCALING : uint
    {
        DISPLAYCONFIG_SCALING_IDENTITY = 1,
        DISPLAYCONFIG_SCALING_CENTERED = 2,
        DISPLAYCONFIG_SCALING_STRETCHED = 3,
        DISPLAYCONFIG_SCALING_ASPECTRATIOCENTEREDMAX = 4,
        DISPLAYCONFIG_SCALING_CUSTOM = 5,
        DISPLAYCONFIG_SCALING_PREFERRED = 128,
        DISPLAYCONFIG_SCALING_FORCE_UINT32 = 0xFFFFFFFF
    }

    public enum DISPLAYCONFIG_PIXELFORMAT : uint
    {
        DISPLAYCONFIG_PIXELFORMAT_8BPP = 1,
        DISPLAYCONFIG_PIXELFORMAT_16BPP = 2,
        DISPLAYCONFIG_PIXELFORMAT_24BPP = 3,
        DISPLAYCONFIG_PIXELFORMAT_32BPP = 4,
        DISPLAYCONFIG_PIXELFORMAT_NONGDI = 5,
        DISPLAYCONFIG_PIXELFORMAT_FORCE_UINT32 = 0xffffffff
    }

    public enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
    {
        DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1,
        DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 2,
        DISPLAYCONFIG_MODE_INFO_TYPE_FORCE_UINT32 = 0xFFFFFFFF
    }

    public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint
    {
        DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3,
        DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4,
        DISPLAYCONFIG_DEVICE_INFO_SET_TARGET_PERSISTENCE = 5,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE = 6,
        DISPLAYCONFIG_DEVICE_INFO_FORCE_UINT32 = 0xFFFFFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
        DISPLAYCONFIG_ROTATION rotation;
        DISPLAYCONFIG_SCALING scaling;
        DISPLAYCONFIG_RATIONAL refreshRate;
        DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
        public bool targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_2DREGION
    {
        public uint cx;
        public uint cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
    {
        public ulong pixelRate;
        public DISPLAYCONFIG_RATIONAL hSyncFreq;
        public DISPLAYCONFIG_RATIONAL vSyncFreq;
        public DISPLAYCONFIG_2DREGION activeSize;
        public DISPLAYCONFIG_2DREGION totalSize;
        public uint videoStandard;
        public DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_TARGET_MODE
    {
        public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTL
    {
        int x;
        int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_SOURCE_MODE
    {
        public uint width;
        public uint height;
        public DISPLAYCONFIG_PIXELFORMAT pixelFormat;
        public POINTL position;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct DISPLAYCONFIG_MODE_INFO_UNION
    {
        [FieldOffset(0)]
        public DISPLAYCONFIG_TARGET_MODE targetMode;
        [FieldOffset(0)]
        public DISPLAYCONFIG_SOURCE_MODE sourceMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_MODE_INFO
    {
        public DISPLAYCONFIG_MODE_INFO_TYPE infoType;
        public uint id;
        public LUID adapterId;
        public DISPLAYCONFIG_MODE_INFO_UNION modeInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
    {
        public uint value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
        public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorDevicePath;
    }

    [DllImport("user32.dll")]
    public static extern int GetDisplayConfigBufferSizes(
        QUERY_DEVICE_CONFIG_FLAGS Flags,
        out uint NumPathArrayElements,
        out uint NumModeInfoArrayElements
    );

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(
        QUERY_DEVICE_CONFIG_FLAGS Flags,
        ref uint NumPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] PathInfoArray,
        ref uint NumModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] ModeInfoArray,
        IntPtr CurrentTopologyId
    );

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName
    );

    [DllImport("iphlpapi.dll", SetLastError = true)]
    internal static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, uint reserved = 0);

    [DllImport("kernel32", SetLastError = true)] // this function is used to get the pointer on the process heap required by AllocateAndGetTcpExTableFromStack
    internal static extern IntPtr GetProcessHeap();
}
