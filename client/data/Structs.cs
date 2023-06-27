using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    [NonSerialized()] public string Caption; // Only used to link partitions, do not serialize.
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
        // [CLEANUP] This can be written better to check the file exists first.
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