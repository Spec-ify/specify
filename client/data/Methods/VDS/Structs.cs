using System;
using System.Runtime.InteropServices;

namespace specify_client.Data.Methods.VDS
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct VDS_DRIVE_LETTER_PROP
    {
        public char wcLetter;
        public Guid volumeId;
        public VDS_DRIVE_LETTER_FLAG ulFlags;
        public bool bUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VDS_DISK_PROP
    {
        public Guid Id;
        public VDS_DISK_STATUS Status;
        public VDS_LUN_RESERVE_MODE ReserveMode;
        public VDS_HEALTH Health;
        public VDS_DEVICE_TYPE DeviceType;
        public VDS_MEDIA_TYPE MediaType;
        public ulong Size;
        public uint BytesPerSector;
        public uint SectorsPerTrack;
        public uint TracksPerCylinder;
        public VDS_DISK_FLAG Flags;
        public VDS_STORAGE_BUS_TYPE BusType;
        public VDS_PARTITION_STYLE PartitionStyle;

        public Guid DiskGuid;
        [MarshalAs(UnmanagedType.LPWStr)] public string DiskAddress;
        [MarshalAs(UnmanagedType.LPWStr)] public string Name;
        [MarshalAs(UnmanagedType.LPWStr)] public string FriendlyName;
        [MarshalAs(UnmanagedType.LPWStr)] public string AdaptorName;
        [MarshalAs(UnmanagedType.LPWStr)] public string DevicePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VDS_ADVANCEDDISK_PROP
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string Id;
        [MarshalAs(UnmanagedType.LPWStr)] public string Pathname;
        [MarshalAs(UnmanagedType.LPWStr)] public string Location;
        [MarshalAs(UnmanagedType.LPWStr)] public string FriendlyName;
        [MarshalAs(UnmanagedType.LPWStr)] public string Identifier;
        public ushort IdentifierFormat;
        public uint Number;
        [MarshalAs(UnmanagedType.LPWStr)] public string SerialNumber;
        [MarshalAs(UnmanagedType.LPWStr)] public string FirmwareVersion;
        [MarshalAs(UnmanagedType.LPWStr)] public string Manufacturer;
        [MarshalAs(UnmanagedType.LPWStr)] public string Model;
        public ulong TotalSize;
        public ulong AllocatedSize;
        public uint LogicalSectorSize;
        public uint PhysicalSectorSize;
        public uint PartitionCount;
        public VDS_DISK_STATUS Status;
        public VDS_HEALTH Health;
        public VDS_STORAGE_BUS_TYPE BusType;
        public VDS_PARTITION_STYLE PartitionStyle;
        public Guid DiskGuid;
        public VDS_DISK_FLAG Flags;
        public VDS_DEVICE_TYPE DeviceType;
    }

    public struct VDS_FILE_SYSTEM_PROP
    {
        public VDS_FILE_SYSTEM_TYPE Type;
        public Guid VolumeId;
        public VDS_FILE_SYSTEM_PROP_FLAG Flags;
        public ulong TotalAllocationUnits;
        public ulong AvailableAllocationUnits;
        public uint AllocationUnitSize;
        [MarshalAs(UnmanagedType.LPWStr)] public string Label;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VDS_VOLUME_PROP
    {
        public Guid Id;
        public VDS_VOLUME_TYPE Type;
        public VDS_VOLUME_STATUS Status;
        public VDS_HEALTH Health;
        public VDS_TRANSITION_STATE TransitionState;
        public ulong Size;
        public VDS_VOLUME_FLAG Flags;
        public VDS_FILE_SYSTEM_TYPE RecommendedFileSystemType;
        [MarshalAs(UnmanagedType.LPWStr)] public string Name;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 80)]
    public struct VDS_DISK_EXTENT
    {
        [FieldOffset(0)] public Guid diskId;
        [FieldOffset(16)] public VDS_DISK_EXTENT_TYPE type;
        [FieldOffset(24)] public ulong Offset;
        [FieldOffset(32)] public ulong Size;
        [FieldOffset(40)] public Guid volumeId;
        [FieldOffset(56)] public Guid plexId;
        [FieldOffset(72)] public uint memberIndex;
    }
}
