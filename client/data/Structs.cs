using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace specify_client.data;

public class NetworkRoute
{
    public List<string> Address = new List<string>();
    public List<int> AverageLatency = new List<int>();
    public List<double> PacketLoss = new List<double>();
}

public class InstalledApp
{
    public string Name;
    public string Version;
    public string InstallDate;
}

public class MicroCode
{
    public string Name;
    public bool Exists;
}

public class StaticCore
{
    public bool On;
}

public class Minidump
{
    public int Count;
}

public class OutputProcess
{
    public string ProcessName;
    public int Count;
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
    public uint? ConfiguredSpeed;

    /** MiB */
    public ulong? Capacity;
}

public class DiskDrive
{
    public string DeviceName;
    public string SerialNumber;
    public UInt32 DiskNumber;
    public ulong? DiskCapacity;
    public ulong? DiskFree;
    public uint? BlockSize;
    public string MediaType;
    public List<Partition> Partitions;
    public List<SmartAttribute> SmartData;
    [NonSerialized()] public string InstanceId; // Only used to link SmartData, do not serialize. Unless you really want to.
}

public class Partition
{
    public ulong PartitionCapacity;
    public ulong PartitionFree;
    public string PartitionLabel;
    public string PartitionLetter;
    public string Filesystem;
    public uint CfgMgrErrorCode;
    public uint LastErrorCode;
    public bool DirtyBitSet;
    public bool BitlockerEncryptionStatus = false;
    [NonSerialized()] public string DeviceId; // Only used to link partitions, do not serialize.
}

public class SmartAttribute
{
    public byte Id;
    public string Name;
    public string RawValue;
    public SmartAttribute(byte id, string name, string rawValue)
    {
        Id = id;
        Name = name;
        RawValue = rawValue;
    }
}

public class TempMeasurement
{
    public string Hardware;
    public string SensorName;
    public float SensorValue;
}

public class NetworkConnection
{
    public string LocalIPAddress;
    public int LocalPort;
    public string RemoteIPAddress;
    public int RemotePort;
    public uint OwningPID;
}

public class BatteryData
{
    public string Name;
    public string Manufacturer;
    public string Chemistry;
    public string Design_Capacity;
    public string Full_Charge_Capacity;
    public string Remaining_Life_Percentage;
}
public class EdidData
{
    // EDID Header Bytes 0-19
    public string FixedHeaderPattern; // Bytes 0-7 - Should always be 0x00FFFFFFFFFFFF00
    public string ManufacturerId; // Bytes 8-9
    public string ProductCode; // Bytes 10-11
    public string SerialNumber; // Bytes 12-15
    public string ManufacturedDate; // Bytes 16-17
    public string EdidVersion; // Byte 18
    public string EdidRevision; // Byte 19

    // Basic Display Parameters Bytes 20-24
    public string VideoInputParametersBitmap; // Byte 20
    public string HorizontalScreenSize; // Byte 21
    public string VerticalScreenSize; // Byte 22
    public string DisplayGamma; // Byte 23
    public string SupportedFeaturesBitmap; // Byte 24

    // Monitor Capabilites Bytes 25-125
    public string ChromacityCoordinates; // Bytes 25-34 - 10-bit CIE 1931 xy coordinates for RGBW
    public string EstablishedTimingBitmap; // Bytes 35-37
    public string TimingInformation; // Bytes 38-53
    public string TimingDescriptors; // Bytes 54-125

    // EDID Footer Bytes 126-127
    public string NumberOfExtensions; // Byte 126
    public string Checksum; // Byte 127
}
public class SensorUpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware) subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor)
    { }

    public void VisitParameter(IParameter parameter)
    { }
}

public interface IRegistryValue
{ }

public class RegistryValue<T> : IRegistryValue
{
    public string HKey;
    public string Path;
    public string Name;
    public T Value;

    public RegistryValue(RegistryKey regKey, string path, string name)
    {
        HKey = regKey.Name;
        Path = path;
        Name = name;
        Value = Utils.GetRegistryValue<T>(regKey, path, name);
    }
}

public class ScheduledTask
{
    public string Path;
    public string Name;

    [JsonConverter(typeof(StringEnumConverter))]
    public TaskState State;

    public bool IsActive;
    public string Author;

    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
    public List<TaskTriggerType> TriggerTypes;

    public ScheduledTask(Task t)
    {
        // A try-catch in a constructor feels like bad form but I'd rather this than double up the error checking inside GetScheduledTasks()
        try
        {
            Name = t.Name;
            Path = t.Path;
            State = t.State;
            IsActive = t.IsActive;
            Author = t.Definition.RegistrationInfo.Author;
            TriggerTypes = t.Definition.Triggers.Select(e => e.TriggerType).ToList();
        }
        catch (FileNotFoundException)
        {
            try
            {
                DebugLog.LogEvent($"A Task is scheduled with a missing or invalid file:", DebugLog.Region.System, DebugLog.EventType.ERROR);
                DebugLog.LogEvent($"{t.Name}", DebugLog.Region.System);
                DebugLog.LogEvent($"{t.Path}", DebugLog.Region.System);
                Name = t.Name;
                Path = t.Path;
                State = default;
                IsActive = default;
                Author = default;
                TriggerTypes = default;
            }
            catch (Exception e)
            {
                DebugLog.LogEvent($"A ScheduledTask failed to enumerate. {e}", DebugLog.Region.System, DebugLog.EventType.ERROR);
            }
        }
    }
}

public class StartupTask
{
    public string AppName;
    public string AppDescription;
    public string ImagePath;
    public DateTime Timestamp;
}

public class Monitor
{
    public string Source;
    public string Name;
    public string ChipType;
    public string DedicatedMemory;
    public string MonitorModel;
    public string CurrentMode;
    public string ConnectionType;
}

public class Browser
{
    public string Name;
    public List<BrowserProfile> Profiles;

    public class BrowserProfile
    {
        public string name;
        public List<Extension> Extensions;
    }

    public class Extension
    {
        public string name;
        public string version;
        public string description;
    }
}

//This is an easy way to serialize data from multiple extension manifest formats without making the Browser object a nightmare
public class ChromiumManifest
{
    public string name;
    public string description;
    public string version;
    public string default_locale;
}

public class PageFile
{
}
public class UnexpectedShutdown
{
    public DateTime? Timestamp;
    public int BugcheckCode;
    public string BugcheckParameter1;
    public string BugcheckParameter2;
    public string BugcheckParameter3;
    public string BugcheckParameter4;
    public ulong PowerButtonTimestamp;
}
public class MachineCheckException
{
    public DateTime? Timestamp;
    public bool MciStatusRegisterValid; // Bit 63
    public bool ErrorOverflow; // Bit 62
    public bool UncorrectedError; // Bit 61
    public bool ErrorReportingEnabled; // Bit 60
    public bool ProcessorContextCorrupted; // Bit 57
    public bool PoisonedData; // Bit 43 - AMD only
    public ushort ExtendedErrorCode; // Bits 16-31
    public string McaErrorCode; // Bits 0-15
    public string ErrorMessage;
    public string TransactionType; // TT
    public string MemoryHeirarchyLevel; // LL
    public string RequestType; // RRRR
    public string Participation; // PP
    public string Timeout; // T
    public string MemoryOrIo; // II
    public string MemoryTransactionType; // MMM
    public string ChannelNumber; // CCCC
}

public class PciWheaError
{
    public DateTime? Timestamp;
    public string VendorId;
    public string DeviceId;
}
public unsafe class WheaErrorRecord
{
    public WheaErrorHeader ErrorHeader;
    public List<WheaErrorDescriptor> ErrorDescriptors = new();
    public List<string> ErrorPackets = new();
}
public class WheaErrorRecordReadable
{
    public WheaErrorHeaderReadable ErrorHeader;
    public List<WheaErrorDescriptorReadable> ErrorDescriptors;
    public List<string> ErrorPackets;
}
public class WheaErrorHeaderReadable
{
    public string Signature;
    public string Revision;
    public string SignatureEnd;
    public string SectionCount;
    public string Severity;
    public string ValidBits;
    public string Length;
    public DateTime Timestamp;
    public string PlatformId;
    public string PartitionId;
    public string CreatorId;
    public string NotifyType;
    public string RecordId;
    public string Flags;
    public string PersistenceInfo;
}
public class WheaErrorDescriptorReadable
{
    public string SectionOffset;
    public string SectionLength;
    public string Revision;
    public string ValidBits;
    // public string Reserved; - I don't think this is useful. Just not gonna bother saving it and needing to display it on Specified.
    public string Flags;
    public string SectionType;
    public string FRUId;
    public string SectionSeverity;
    public string FRUText;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct WheaErrorHeader
{
    public uint Signature;
    public ushort Revision;
    public uint SignatureEnd;
    public ushort SectionCount;
    public WheaSeverity Severity;
    public uint ValidBits;
    public uint Length;
    public ulong Timestamp;
    public Guid PlatformId;
    public Guid PartitionId;
    public Guid CreatorId;
    public Guid NotifyType;
    public ulong RecordId;
    public uint Flags;
    public ulong PersistenceInfo;
    public uint Reserved1;
    public ulong Reserved2;
    public static WheaErrorHeader FromBytes(byte[] bytes)
    {
        fixed (byte* pData = bytes)
        {
            return Unsafe.ReadUnaligned<WheaErrorHeader>(pData);
        }
    }
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct WheaErrorDescriptor
{
    public uint SectionOffset;
    public uint SectionLength;
    public ushort Revision;
    public byte ValidBits;
    public byte Reserved;
    public uint Flags;
    public Guid SectionType;
    public Guid FRUId;
    public WheaSeverity SectionSeverity;
    public fixed byte FRUText[20];

    public static WheaErrorDescriptor FromBytes(byte[] bytes)
    {
        fixed (byte* pData = bytes)
        {
            return Unsafe.ReadUnaligned<WheaErrorDescriptor>(pData);
        }
    }
}

public enum WheaSeverity
{
    Corrected,
    Fatal,
    Warning,
    Information
};
public enum SimpleErrorCodes : ulong
{
    NoError = 0b0000,
    Unclassified = 0b0001,
    Microcode = 0b0010,
    External = 0b0011,
    FrcError = 0b0100,
    InternalParity = 0b0101,
    SmmHandler = 0b0110,
    InternalTimer = 0b0000010000000000,
    IoError = 0b0000111000001011
}