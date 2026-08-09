using System;
using System.Runtime.InteropServices;

namespace specify_client.Data.Methods.VDS
{
    public enum VDS_DISK_EXTENT_TYPE : int
    {
        UNKNOWN = 0x00000000,
        FREE = 0x00000001,
        DATA = 0x00000002,
        OEM = 0x00000003,
        ESP = 0x00000004,
        MSR = 0x00000005,
        LDM = 0x00000006,
        UNUSABLE = 0x00007FFF
    }

    public enum VDS_VOLUME_TYPE : uint
    {
        UNKNOWN = 0x00000000,
        SIMPLE = 0x0000000A,
        SPAN = 0x0000000B,
        STRIPE = 0x0000000C,
        MIRROR = 0x0000000D,
        PARITY = 0x0000000E
    }

    public enum VDS_VOLUME_STATUS : uint
    {
        UNKNOWN = 0x00000000,
        ONLINE = 0x00000001,
        NO_MEDIA = 0x00000003,
        OFFLINE = 0x00000004,
        FAILED = 0x00000005
    }

    [Flags]
    public enum VDS_VOLUME_FLAG : uint
    {
        SYSTEM_VOLUME = 0x00000001,
        BOOT_VOLUME = 0x00000002,
        ACTIVE = 0x00000004,
        READONLY = 0x00000008,
        HIDDEN = 0x00000010,
        CAN_EXTEND = 0x00000020,
        CAN_SHRINK = 0x00000040,
        PAGEFILE = 0x00000080,
        HIBERNATION = 0x00000100,
        CRASHDUMP = 0x00000200,
        INSTALLABLE = 0x00000400,
        LBN_REMAP_ENABLED = 0x00000800,
        FORMATTING = 0x00001000,
        NOT_FORMATTABLE = 0x00002000,
        NTFS_NOT_SUPPORTED = 0x00004000,
        FAT32_NOT_SUPPORTED = 0x00008000,
        FAT_NOT_SUPPORTED = 0x00010000,
        NO_DEFAULT_DRIVE_LETTER = 0x00020000,
        PERMANENTLY_DISMOUNTED = 0x00040000,
        PERMANENT_DISMOUNT_SUPPORTED = 0x00080000,
        SHADOW_COPY = 0x00100000,
        FVE_ENABLED = 0x00200000,
        DIRTY = 0x00400000,
        REFS_NOT_SUPPORTED = 0x00800000
    }

    public enum VDS_TRANSITION_STATE : uint
    {
        UNKNOWN = 0x00000000,
        STABLE = 0x00000001,
        EXTENDING = 0x00000002,
        SHRINKING = 0x00000003,
        RECONFIGING = 0x00000004
    }

    public enum VDS_FILE_SYSTEM_TYPE
    {
        UNKNOWN = 0x00000000,
        RAW = 0x00000001,
        FAT = 0x00000002,
        FAT32 = 0x00000003,
        NTFS = 0x00000004,
        CDFS = 0x00000005,
        UDF = 0x00000006,
        EXFAT = 0x00000007,
        CSVFS = 0x00000008,
        REFS = 0x00000009
    }

    [Flags]
    public enum VDS_PROVIDER_FLAG : uint
    {
        DYNAMIC = 0x1,
        INTERNAL_HARDWARE_PROVIDER = 0x2,
        ONE_DISK_ONLY_PER_PACK = 0x4,
        ONE_PACK_ONLINE_ONLY = 0x8,
        VOLUME_SPACE_MUST_BE_CONTIGUOUS = 0x10,
        SUPPORT_DYNAMIC = 0x80000000,
        SUPPORT_FAULT_TOLERANT = 0x40000000,
        SUPPORT_DYNAMIC_1394 = 0x20000000,
        SUPPORT_MIRROR = 0x20,
        SUPPORT_RAID5 = 0x40
    }

    public enum VDS_PROVIDER_TYPE : uint
    {
        UNKNOWN = 0,
        SOFTWARE = 1,
        HARDWARE = 2,
        VIRTUALDISK = 3,
        MAX = 4
    }

    public enum VDS_PARTITION_STYLE
    {
        UNKNOWN = 0x00000000,
        MBR = 0x00000001,
        GPT = 0x00000002
    }

    public enum VDS_STORAGE_BUS_TYPE : uint
    {
        Unknown = 0,
        Scsi = 0x1,
        Atapi = 0x2,
        Ata = 0x3,
        IEEE1394 = 0x4,
        Ssa = 0x5,
        Fibre = 0x6,
        Usb = 0x7,
        RAID = 0x8,
        iScsi = 0x9,
        Sas = 0xa,
        Sata = 0xb,
        Sd = 0xc,
        Mmc = 0xd,
        Max = 0xe,
        Virtual = 0xe,
        FileBackedVirtual = 0xf,
        Spaces = 0x10,
        NVMe = 0x11,
        Scm = 0x12,
        Ufs = 0x13,
        MaxReserved = 0x7f
    }

    [Flags]
    public enum VDS_DISK_FLAG
    {
        AUDIO_CD = 0x1,
        HOTSPARE = 0x2,
        RESERVE_CAPABLE = 0x4,
        MASKED = 0x8,
        STYLE_CONVERTIBLE = 0x10,
        CLUSTERED = 0x20,
        READ_ONLY = 0x40,
        SYSTEM_DISK = 0x80,
        BOOT_DISK = 0x100,
        PAGEFILE_DISK = 0x200,
        HIBERNATIONFILE_DISK = 0x400,
        CRASHDUMP_DISK = 0x800,
        HAS_ARC_PATH = 0x1000,
        DYNAMIC = 0x2000,
        BOOT_FROM_DISK = 0x4000,
        CURRENT_READ_ONLY = 0x8000
    }

    public enum VDS_DISK_STATUS
    {
        UNKNOWN = 0x00000000,
        ONLINE = 0x00000001,
        NOT_READY = 0x00000002,
        NO_MEDIA = 0x00000003,
        OFFLINE = 0x00000004,
        FAILED = 0x00000005,
        MISSING = 0x00000006
    }

    public enum VDS_DEVICE_TYPE : uint
    {
        CD_ROM = 0x00000002,
        DISK = 0x00000007,
        DVD = 0x00000033
    }

    public enum VDS_MEDIA_TYPE : uint
    {
        Unknown = 0x00000000,
        RemovableMedia = 0x0000000B,
        FixedMedia = 0x0000000C
    }

    public enum VDS_HEALTH : uint
    {
        UNKNOWN = 0x00000000,
        HEALTHY = 0x00000001,
        REBUILDING = 0x00000002,
        STALE = 0x00000003,
        FAILING = 0x00000004,
        FAILING_REDUNDANCY = 0x00000005,
        FAILED_REDUNDANCY = 0x00000006,
        FAILED_REDUNDANCY_FAILING = 0x00000007,
        FAILED = 0x00000008
    }

    public enum VDS_LUN_RESERVE_MODE : uint
    {
        NONE = 0x00000000,
        EXCLUSIVE_RW = 0x00000001,
        EXCLUSIVE_RO = 0x00000002,
        SHARED_RO = 0x00000003,
        SHARED_RW = 0x00000004
    }

    public enum VDS_DRIVE_LETTER_FLAG : uint
    {
        NON_PERSISTENT = 0x1
    }

    public enum VDS_QUERY_PROVIDER_FLAG : uint
    {
        SOFTWARE_PROVIDERS = 0x1,
        HARDWARE_PROVIDERS = 0x2,
        VIRTUALDISK_PROVIDERS = 0x4
    }

    [Flags]
    public enum VDS_GPT_ATTRIBUTES : ulong
    {
        /// <summary>
        /// Partition is required for the platform to function properly
        /// </summary>
        ATTRIBUTE_PLATFORM_REQUIRED = 0x0000000000000001,
        /// <summary>
        /// Partition cannot be written to but can be read from. Used only with the basic data partition type
        /// </summary>
        BASIC_DATA_ATTRIBUTE_READ_ONLY = 0x1000000000000000,
        /// <summary>
        /// Partition is a shadow copy. Used only with the basic data partition type.
        /// </summary>
        BASIC_DATA_ATTRIBUTE_SHADOW_COPY = 0x2000000000000000,
        /// <summary>
        /// Partition is hidden and will not be mounted. Used only with the basic data partition type.
        /// </summary>
        BASIC_DATA_ATTRIBUTE_HIDDEN = 0x4000000000000000,
        /// <summary>
        /// Partition does not receive a drive letter by default when moving the disk to another machine. Used only with the basic data partition type.
        /// </summary>
        BASIC_DATA_ATTRIBUTE_NO_DRIVE_LETTER = 0x8000000000000000
    }

    [Flags]
    public enum VDS_FILE_SYSTEM_PROP_FLAG : uint
    {
        COMPRESSED = 0x00000001
    }
}
