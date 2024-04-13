using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Channels;
using System.Runtime.InteropServices.ComTypes;

namespace specify_client.data;

using static specify_client.DebugLog;
using static Utils;

public static partial class Cache
{
    public static async Task MakeEventData()
    {
        try
        {
            Region region = Region.Events;
            await StartRegion(region);

            List<Task> eventTaskList = new()
            {
                DoTask(region, "GetUnexpectedShutdowns", GetUnexpectedShutdowns),
                DoTask(region, "GetWheaEvents", GetWheaEvents)
            };

            await Task.WhenAll(eventTaskList);
            await EndRegion(region);
        }
        catch (Exception ex)
        {
            await LogFatalError($"{ex}", Region.Events);
        }
    }
    public static async Task GetUnexpectedShutdowns()
    {
        string eventLogName = "System";
        int targetEventId = 41;
        string query = $"*[System[EventID={targetEventId}]]";
        var eventLogQuery = new EventLogQuery(eventLogName, PathType.LogName, query);
        eventLogQuery.ReverseDirection = true;

        using (EventLogReader logReader = new EventLogReader(eventLogQuery))
        {
            int maxEvents = 10; // I don't think we need more than 10. The cursed powershell command only grabs 10.
            UnexpectedShutdowns = new();

            for (EventRecord eventInstance = logReader.ReadEvent(); eventInstance != null; eventInstance = logReader.ReadEvent())
            {
                if (UnexpectedShutdowns.Count >= maxEvents)
                {
                    break;
                }

                UnexpectedShutdown shutdown = new();

                shutdown.Timestamp = eventInstance.TimeCreated;

                XmlNode eventDataNode = GetDataNode(eventInstance);

                if (eventDataNode != null)
                {
                    foreach (XmlNode dataNode in eventDataNode.ChildNodes)
                    {
                        if (dataNode.Name == "Data")
                        {
                            string dataName = dataNode.Attributes["Name"].Value;
                            string dataValue = dataNode.InnerText;

                            if (dataName.Equals("BugcheckCode"))
                            {
                                int.TryParse(dataValue, out shutdown.BugcheckCode);
                            }
                            if (dataName.Equals("BugcheckParameter1"))
                            {
                                shutdown.BugcheckParameter1 = dataValue;
                            }
                            if (dataName.Equals("BugcheckParameter2"))
                            {
                                shutdown.BugcheckParameter2 = dataValue;
                            }
                            if (dataName.Equals("BugcheckParameter3"))
                            {
                                shutdown.BugcheckParameter3 = dataValue;
                            }
                            if (dataName.Equals("BugcheckParameter4"))
                            {
                                shutdown.BugcheckParameter4 = dataValue;
                            }
                            if (dataName.Equals("PowerButtonTimestamp"))
                            {
                                ulong.TryParse(dataValue, out shutdown.PowerButtonTimestamp);
                            }
                        }
                    }
                }
                UnexpectedShutdowns.Add(shutdown);
            }
        }
    }
    public static async Task GetWheaEvents()
    {
        string eventLogName = "System";
        string eventSource = "Microsoft-Windows-WHEA-Logger";
        string query = $"*[System/Provider[@Name=\"{eventSource}\"]]";
        var eventLogQuery = new EventLogQuery(eventLogName, PathType.LogName, query);
        eventLogQuery.ReverseDirection = true;

        using (EventLogReader logReader = new EventLogReader(eventLogQuery))
        {
            /* There are three main types of WHEA errors from the WHEA-Logger in Event Viewer:
             * Machine Check Exceptions, which have an MCI Status number to be decoded
             * PCI Device Errors, which only have a Vendor ID and Device ID. Can we call an API to translate these directly?
             * WHEA Error Records, which have a "Raw Data" string that can be translated.
             */

            int maxEvents = 10;

            MachineCheckExceptions = new();
            PciWheaErrors = new();
            WheaErrorRecords = new();

            for (EventRecord eventInstance = logReader.ReadEvent(); eventInstance != null; eventInstance = logReader.ReadEvent())
            {
                if (MachineCheckExceptions.Count >= maxEvents ||
                    PciWheaErrors.Count >= maxEvents ||
                    WheaErrorRecords.Count >= maxEvents)
                {
                    break;
                }

                XmlNode eventDataNode = GetDataNode(eventInstance);

                if (eventDataNode != null)
                {
                    foreach (XmlNode dataNode in eventDataNode.ChildNodes)
                    {
                        if (dataNode.Name == "Data")
                        {
                            var dataName = dataNode.Attributes["Name"].Value;

                            if (dataName.Contains("MciStat"))
                            {
                                // MCE
                                var timestamp = eventInstance.TimeCreated;
                                MachineCheckExceptions.Add(MakeMachineCheckException(dataNode, timestamp));
                            }
                            if (dataName.Contains("PrimaryDeviceName"))
                            {
                                // PCIe Error
                                PciWheaErrors.Add(MakePcieError(eventInstance, eventDataNode));
                            }
                            if (dataName.Contains("RawData"))
                            {
                                // WHEA Record
                                WheaErrorRecords.Add(MakeWheaErrorRecord(dataNode));
                            }
                        }
                    }
                }
            }
        }
    }
    public static MachineCheckException MakeMachineCheckException(XmlNode dataNode, DateTime timestamp)
    {
        MachineCheckException mce = new();
        mce.Timestamp = timestamp;

        string MciStatString = dataNode.InnerText.Substring(2);

        if (!ulong.TryParse(MciStatString, System.Globalization.NumberStyles.HexNumber, null, out ulong MciStat))
        {
            LogEvent($"MciStat string invalid: {MciStatString}", Region.Events, EventType.ERROR);
            return mce;
        }

        // We call WMI to get cpu a second time to avoid a race condition.
        var cpu = GetWmi("Win32_Processor").FirstOrDefault();

        cpu.TryWmiRead("Manufacturer", out string cpuManufacturer);

        if (cpuManufacturer.Contains("Intel"))
        {
            mce = MakeIntelMce(MciStat);
        }
        else if (cpuManufacturer.Contains("AMD"))
        {
            mce = MakeAmdMce(MciStat);
        }
        else
        {
            LogEvent($"Unknown CPU Manufacturer: {cpuManufacturer}", Region.Events, EventType.ERROR);
        }

        return mce;
    }
    public static MachineCheckException MakeIntelMce(ulong MciStat)
    {
        MachineCheckException mce = GetCommonMceProperties(MciStat);

        mce = TranslateIntelMceErrorCode(mce, MciStat);

        return mce;
    }
    public static MachineCheckException MakeAmdMce(ulong MciStat)
    {
        MachineCheckException mce = GetCommonMceProperties(MciStat);

        mce.PoisonedData = CheckBitMask(MciStat, 1ul << 43);

        mce = TranslateAmdMceErrorCode(mce, MciStat);

        return mce;
    }
    public static MachineCheckException TranslateIntelMceErrorCode(MachineCheckException mce, ulong MciStat)
    {
        /*
         *  0000 0000 0000 0000 - No Error - No error has been reported to this bank of error-reporting registers.
            0000 0000 0000 0001 - Unclassified - This error has not been classified into the MCA error classes.
            0000 0000 0000 0010 - Microcode ROM Parity Error - Parity error in internal microcode ROM
            0000 0000 0000 0011 - External Error - The BINIT# from another processor caused this processor to enter machine check.
            0000 0000 0000 0100 - FRC Error - FRC (functional redundancy check) main/secondary error.
            0000 0000 0000 0101 - Internal Parity Error - Internal parity error.
            0000 0000 0000 0110 - SMM Handler Code Access Violation - An attempt was made by the SMM Handler to execute outside the ranges specified by SMRR.
            0000 0100 0000 0000 - Internal Timer Error - Internal timer error. 
            0000 1110 0000 1011 - I/O Error - generic I/O error. 
            0000 01xx xxxx xxxx - Internal Unclassified - Internal unclassified errors. */



        /*
         *  000F 0000 0000 11LL - Generic Cache Hierarchy - Generic cache hierarchy error
            000F 0000 0001 TTLL - TLB Errors - {TT}TLB{LL}_ERR
            000F 0000 1MMM CCCC - Memory Controller Errors - {MMM}_CHANNEL{CCCC}_ERR 
            000F 0001 RRRR TTLL - Cache Hierarchy Errors - {TT}CACHE{LL}_{RRRR}_ERR 
            000F 0010 1MMM CCCC - Extended Memory Errors - {MMM}_CHANNEL{CCCC}_ERR 
            000F 1PPT RRRR IILL - Bus and Interconnect Errors  BUS{LL}_{PP}_{RRRR}_{II}_{T}_ERR */



        var code = MciStat & 0xFFFF;

        ParseIntelMciStatus(code, out string LL, out string TT, out string II, out string CCCC, out string RRRR, out string MMM, out string PP, out string Timeout);

        // I/O Error
        if (code == (ulong)SimpleErrorCodes.IoError)
        {
            mce.ErrorMessage = "I/O Error - Generic I/O Error";
        }
        // Bus and Interconnect Errors
        else if (CheckBitMask(code, 1ul << 11))
        {
            mce.ErrorMessage = "Bus and Interconnect Error";
            mce.MemoryHeirarchyLevel = LL;
            mce.Participation = PP;
            mce.RequestType = RRRR;
            mce.MemoryOrIo = II;
            mce.Timeout = Timeout;
        }
        // Internal Timer
        else if (code == (ulong)SimpleErrorCodes.InternalTimer)
        {
            mce.ErrorMessage = "Internal Timer Error - Internal timer error"; // Message is duplicated in the manual. I kept it for consistency's sake.
        }
        // Internal Unclassified Errors
        else if (CheckBitMask(code, 1ul << 10))
        {
            mce.ErrorMessage = "Internal Unclassified - Internal unclassified errors";
        }
        // Extended Memory Errors
        else if (CheckBitMask(code, 1ul << 9))
        {
            mce.ErrorMessage = "Extended Memory Error";
            mce.MemoryTransactionType = MMM;
            mce.ChannelNumber = CCCC;
        }
        // Cache Hierarchy Errors
        else if (CheckBitMask(code, 1ul << 8))
        {
            mce.ErrorMessage = "Cache Hierarchy Error";
            mce.TransactionType = TT;
            mce.MemoryHeirarchyLevel = LL;
            mce.RequestType = RRRR;
        }
        // Memory Controller Errors
        else if (CheckBitMask(code, 1ul << 7))
        {
            mce.ErrorMessage = "Memory Controller Error";
            mce.MemoryTransactionType = MMM;
            mce.ChannelNumber = CCCC;
        }
        // TLB Errors
        else if (CheckBitMask(code, 1ul << 4))
        {
            mce.ErrorMessage = "Translation Lookaside Buffer (TLB) Error";
            mce.TransactionType = TT;
            mce.MemoryHeirarchyLevel = LL;
        }
        // Generic Cache Heirarchy Error
        else if (CheckBitMask(code, 0b11ul << 2))
        {
            mce.ErrorMessage = "Generic Cache Heirarchy Error";
            mce.MemoryHeirarchyLevel = LL;
        }
        // No error
        else if (code == (ulong)SimpleErrorCodes.NoError)
        {
            mce.ErrorMessage = "No Error - No error has been reported to this bank of error-reporting registers"; // ??
        }
        // Unclassified error
        else if (code == (ulong)SimpleErrorCodes.Unclassified)
        {
            mce.ErrorMessage = "Unclassified - This error has not been classified into the MCA error classes.";
        }
        // Microcode ROM Parity Error
        else if (code == (ulong)SimpleErrorCodes.Microcode)
        {
            mce.ErrorMessage = "Microcode ROM Parity Error - Parity error in internal microcode ROM";
        }
        // External Error
        else if (code == (ulong)SimpleErrorCodes.External)
        {
            mce.ErrorMessage = "External Error - The BINIT# from another processor caused this processor to enter machine check.";
        }
        // FRC Error
        else if (code == (ulong)SimpleErrorCodes.FrcError)
        {
            mce.ErrorMessage = "FRC Error - FRC (functional redundancy check) main/secondary error.";
        }
        // Internal Parity Error
        else if (code == (ulong)SimpleErrorCodes.InternalParity)
        {
            mce.ErrorMessage = "Internal Parity Error - Internal parity error.";
        }
        // SMM Handler Code Access Violation
        else if (code == (ulong)SimpleErrorCodes.SmmHandler)
        {
            mce.ErrorMessage = "SMM Handler Code Access Violation - An attempt was made by the SMM Handler to execute outside the ranges specified by SMRR.";
        }
        else
        {
            mce.ErrorMessage = "Invalid Error Code";
        }
        return mce;
    }
    public static MachineCheckException TranslateAmdMceErrorCode(MachineCheckException mce, ulong MciStat)
    {
        /*
         *  0000 0000 0001 TTLL 
         *  TLB 
         *  TT = Transaction Type
            LL = Cache Level

            0000 0001 RRRR TTLL 
            Memory 
            RRRR = Memory Transaction Type
            TT = Transaction Type
            LL = Cache Level

            0000 1XXT RRRR XXLL 
            Bus 
            XX = Reserved
            T = Timeout
            RRRR = Memory Transaction Type
            LL = Cache Level

            0000 01UU 0000 0000 
            Internal Unclassified
            UU = Internal Error Type*/

        var code = MciStat & 0xFFFF;

        ParseAmdMciStatus(code, out string LL, out string TT, out string RRRR, out string Timeout);

        // Bus Error
        if (CheckBitMask(MciStat, 1ul << 11))
        {
            mce.ErrorMessage = "Bus Error";
            mce.Timeout = Timeout;
            mce.RequestType = RRRR;
            mce.MemoryHeirarchyLevel = LL;
        }
        // Internal Error
        else if (CheckBitMask(MciStat, 1ul << 10))
        {
            mce.ErrorMessage = "Internal Unclassified Error";
        }

        // Memory Controller Error
        else if (CheckBitMask(MciStat, 1ul << 8))
        {
            mce.ErrorMessage = "Memory Controller Error";
            mce.RequestType = RRRR;
            mce.TransactionType = TT;
            mce.MemoryHeirarchyLevel = LL;
        }
        // TLB Error
        else if (CheckBitMask(MciStat, 1ul << 4))
        {
            mce.ErrorMessage = "Translation Lookaside Buffer (TLB) Error";
            mce.TransactionType = TT;
            mce.MemoryHeirarchyLevel = LL;
        }
        // Invalid Error Code
        else
        {
            mce.ErrorMessage = "Invalid Error Code";
            LogEvent($"AMD MciStatus Code Invalid - MciStat: 0x{code:X4}", Region.Events, EventType.ERROR);
        }
        return mce;
    }
    public static void ParseIntelMciStatus(ulong errorCode, out string LL, out string TT, out string II, out string CCCC, out string RRRR, out string MMM, out string PP, out string Timeout)
    {
        var ll = errorCode & 0b11;
        var tt = (errorCode & 0b1100) >> 2;
        var ii = tt;
        var cccc = errorCode & 0b1111;
        var rrrr = (errorCode & 0b11110000) >> 4;
        var mmm = (errorCode & 0b1110000) >> 4;
        var pp = (errorCode & 0b11000000000) >> 9;

        Timeout = CheckBitMask(errorCode, 1ul << 8).ToString();

        switch (ll)
        {
            case 0:
                LL = "L0: Core";
                break;
            case 1:
                LL = "L1: Level 1";
                break;
            case 2:
                LL = "L2: Level 2";
                break;
            default:
                LL = "LG: Generic";
                break;
        }
        switch (tt)
        {
            case 0:
                TT = "Instruction";
                break;
            case 1:
                TT = "Data";
                break;
            case 2:
                TT = "Generic";
                break;
            default:
                TT = "Reserved";
                break;
        }
        switch (ii)
        {
            case 0:
                II = "Memory Access";
                break;

            case 2:
                II = "I/O";
                break;
            case 3:
                II = "Other transaction"; // ??
                break;
            default: // Should be 0b01
                II = "Reserved/Invalid";
                break;
        }
        if (cccc != 7)
        {
            CCCC = cccc.ToString();
        }
        else
        {
            CCCC = "Unspecified";
        }
        switch (rrrr)
        {
            case 0:
                RRRR = "Generic";
                break;
            case 1:
                RRRR = "Generic Read";
                break;
            case 2:
                RRRR = "Generic Write";
                break;
            case 3:
                RRRR = "Data Read";
                break;
            case 4:
                RRRR = "Data Write";
                break;
            case 5:
                RRRR = "Instruction Fetch";
                break;
            case 6:
                RRRR = "Prefetch";
                break;
            case 7:
                RRRR = "Evict";
                break;
            case 8:
                RRRR = "Snoop (Probe)";
                break;
            default:
                RRRR = "INVALID";
                LogEvent($"Memory Transaction Type Invalid: RRRR: {rrrr} - ErrorCode: 0x{errorCode:X4}", Region.Events, EventType.ERROR);
                break;


        }
        switch (mmm)
        {
            case 0:
                MMM = "Generic Undefined Request";
                break;
            case 1:
                MMM = "Memory Read Error";
                break;
            case 2:
                MMM = "Memory Write Error";
                break;
            case 3:
                MMM = "Address/Command Error";
                break;
            case 4:
                MMM = "Memory Scrubbing Error";
                break;
            default:
                MMM = "Reserved/Invalid";
                break;
        }
        switch (pp)
        {
            case 0:
                PP = "Local processor originated request";
                break;
            case 1:
                PP = "Local processor responded to request";
                break;
            case 2:
                PP = "Local processor observed as third party";
                break;
            default:
                PP = "Generic";
                break;
        }
    }
    public static void ParseAmdMciStatus(ulong errorCode, out string LL, out string TT, out string RRRR, out string Timeout)
    {
        var ll = errorCode & 0b11;
        var tt = (errorCode & 0b1100) >> 2;
        var rrrr = (errorCode & 0b11110000) >> 4;

        Timeout = CheckBitMask(errorCode, 1ul << 8).ToString();

        switch (ll)
        {
            case 0:
                LL = "L0: Core";
                break;
            case 1:
                LL = "L1: Level 1";
                break;
            case 2:
                LL = "L2: Level 2";
                break;
            default:
                LL = "LG: Generic";
                break;
        }
        switch (tt)
        {
            case 0:
                TT = "Instruction";
                break;
            case 1:
                TT = "Data";
                break;
            case 2:
                TT = "Generic";
                break;
            default:
                TT = "Reserved";
                break;
        }
        switch (rrrr)
        {
            case 0:
                RRRR = "Generic";
                break;
            case 1:
                RRRR = "Generic Read";
                break;
            case 2:
                RRRR = "Generic Write";
                break;
            case 3:
                RRRR = "Data Read";
                break;
            case 4:
                RRRR = "Data Write";
                break;
            case 5:
                RRRR = "Instruction Fetch";
                break;
            case 6:
                RRRR = "Prefetch";
                break;
            case 7:
                RRRR = "Evict";
                break;
            case 8:
                RRRR = "Snoop (Probe)";
                break;
            default:
                RRRR = "INVALID";
                LogEvent($"Memory Transaction Type Invalid: RRRR: {rrrr} - ErrorCode: 0x{errorCode:X4}", Region.Events, EventType.ERROR);
                break;
        }
    }
    public static MachineCheckException GetCommonMceProperties(ulong MciStat)
    {
        MachineCheckException mce = new();

        mce.MciStatusRegisterValid = CheckBitMask(MciStat, 1ul << 63);
        mce.ErrorOverflow = CheckBitMask(MciStat, 1ul << 62);
        mce.UncorrectedError = CheckBitMask(MciStat, 1ul << 61);
        mce.ErrorReportingEnabled = CheckBitMask(MciStat, 1ul << 60);
        mce.ProcessorContextCorrupted = CheckBitMask(MciStat, 1ul << 57);

        // The extended error code on MCI Status codes are bits 16-31. We mask the MciStat to get just those bits and then bitshift it right to get the full code.
        var maskedCode = MciStat & 0xFFFF0000;
        var bitshiftedCode = maskedCode >> 16;

        mce.ExtendedErrorCode = (ushort)bitshiftedCode;

        string errorCode = MakeMcaErrorCode(MciStat);

        mce.McaErrorCode = errorCode;

        return mce;
    }
    public static string MakeMcaErrorCode(ulong MciStat)
    {
        // Convert the lower 16 bits of the MciStatus code to a binary string
        StringBuilder binary = new(Convert.ToString((long)(MciStat & 0xFFFF), 2).PadLeft(16, '0'));

        // Add spaces every four bits
        binary.Insert(12, ' ');
        binary.Insert(8, ' ');
        binary.Insert(4, ' ');

        return binary.ToString().Trim();
    }
    public static PciWheaError MakePcieError(EventRecord eventInstance, XmlNode eventDataNode)
    {
        PciWheaError error = new();
        error.Timestamp = eventInstance.TimeCreated;
        foreach (XmlNode dataNode in eventDataNode.ChildNodes)
        {
            if (dataNode.Name == "Data")
            {
                var dataName = dataNode.Attributes["Name"].Value;
                var dataValue = dataNode.InnerText;
                if (dataName.Equals("VendorID"))
                {
                    error.VendorId = dataValue;
                }
                if (dataName.Equals("DeviceID"))
                {
                    error.DeviceId = dataValue;
                }
            }
        }
        return error;
    }
    public static WheaErrorRecordReadable MakeWheaErrorRecord(XmlNode dataNode)
    {
        WheaErrorRecord record = new();

        var rawData = dataNode.InnerText;

        var WheaByteArray = ByteArrayFromHexString(rawData);

        var WheaHeader = WheaErrorHeader.FromBytes(WheaByteArray);
        record.ErrorHeader = WheaHeader;

        for (int i = 0; i < WheaHeader.SectionCount; i++)
        {
            WheaErrorDescriptor descriptor = GetWheaDescriptor(WheaByteArray, i);
            string errorPacket = GetWheaErrorPacket(WheaByteArray, descriptor);
            record.ErrorDescriptors.Add(descriptor);
            record.ErrorPackets.Add(errorPacket);
        }
        return MakeReadableErrorRecord(record, WheaByteArray);
    }
    public static WheaErrorRecordReadable MakeReadableErrorRecord(WheaErrorRecord record, byte[] wheaByteArray)
    {
        var readableError = new WheaErrorRecordReadable();

        readableError.ErrorHeader = MakeReadableErrorHeader(record.ErrorHeader, wheaByteArray);
        readableError.ErrorDescriptors = MakeReadableErrorDescriptors(record.ErrorDescriptors);
        readableError.ErrorPackets = record.ErrorPackets;

        return readableError;
    }
    public static WheaErrorHeaderReadable MakeReadableErrorHeader(WheaErrorHeader header, byte[] wheaByteArray)
    {
        var readableHeader = new WheaErrorHeaderReadable();
        if (!CheckWheaErrorValidity(header, wheaByteArray, out readableHeader.Signature, out readableHeader.Revision, out readableHeader.SignatureEnd, out readableHeader.Length))
        {
            // Should this be an error? There are cases when an error record would be invalid without some sort of windows failure.
            LogEvent($"Whea error record is invalid.", Region.Events, EventType.WARNING);
        }

        readableHeader.SectionCount = header.SectionCount.ToString();
        readableHeader.Severity = header.Severity.ToString();
        readableHeader.Length = $"0x{header.Length:X8}";
        readableHeader.ValidBits = $"0x{header.ValidBits:X8}"; // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_whea_error_record_header_validbits - They are not particularly relevant.
        readableHeader.Timestamp = TranslateWheaTimestamp(header.Timestamp);
        readableHeader.PlatformId = header.PlatformId.ToString();
        readableHeader.PartitionId = header.PartitionId.ToString();

        // Translate GUIDs
        readableHeader.CreatorId = GetCreatorId(header.CreatorId);
        readableHeader.NotifyType = GetNotifyType(header.NotifyType);

        readableHeader.RecordId = header.RecordId.ToString();
        readableHeader.Flags = $"0x{header.Flags:X8}"; // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-whea_error_record_header_flags
        readableHeader.PersistenceInfo = $"0x{header.PersistenceInfo:X16}"; // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_whea_persistence_info

        return readableHeader;
    }
    public static string GetNotifyType(Guid notifyType)
    {
        string type = notifyType.ToString();
        return type switch
        {
            "2dce8bb1-bdd7-450e-b9ad-9cf4ebd4f890" =>
                "CMC_NOTIFY_TYPE_GUID",

            "4e292f96-d843-4a55-a8c2-d481f27ebeee" =>
                "CPE_NOTIFY_TYPE_GUID",

            "e8f56ffe-919c-4cc5-ba88-65abe14913bb" =>
                "MCE_NOTIFY_TYPE_GUID",

            "cf93c01f-1a16-4dfc-b8bc-9c4daf67c104" =>
                "PCIe_NOTIFY_TYPE_GUID",

            "cc5263e8-9308-454a-89d0-340bd39bc98e" =>
                "INIT_NOTIFY_TYPE_GUID",

            "5bad89ff-b7e6-42c9-814a-cf2485d6e98a" =>
                "NMI_NOTIFY_TYPE_GUID",

            "3d61a466-ab40-409a-a698-f362d464b38f" =>
                "BOOT_NOTIFY_TYPE_GUID",

            "9a78788a-bbe8-11e4-809e-67611e5d46b0" =>
                "SEA_NOTIFY_TYPE_GUID",

            "5c284c81-b0ae-4e87-a322-b04c85624323" =>
                "SEA_NOTIFY_TYPE_GUID",

            "09a9d5ac-5204-4214-96e5-94992e752bcd" =>
                "PEI_NOTIFY_TYPE_GUID",

            "487565ba-6494-4367-95ca-4eff893522f6" =>
                "BMC_NOTIFY_TYPE_GUID",

            "e9d59197-94ee-4a4f-8ad8-9b7d8bd93d2e" =>
                "SCI_NOTIFY_TYPE_GUID",

            "fe84086e-b557-43cf-ac1b-17982e078470" =>
                "EXTINT_NOTIFY_TYPE_GUID",

            "0033f803-2e70-4e88-992c-6f26daf3db7a" =>
                "DEVICE_DRIVER_NOTIFY_TYPE_GUID",

            "919448b2-3739-4b7f-a8f1-e0062805c2a3" =>
                "CMCI_NOTIFY_TYPE_GUID",

            "00000000-0000-0000-0000-000000000000" =>
                "CPER_EMPTY_GUID",

            _ => "INVALID GUID"
        };
    }
    public static string GetCreatorId(Guid creatorId)
    {
        string id = creatorId.ToString();
        return id switch
        {
            "cf07c4bd-b789-4e18-b3c4-1f732cb57131" =>
                "WHEA_RECORD_CREATOR_GUID",

            "57217c8d-5e66-44fb-8033-9b74cacedf5b" =>
                "DEFAULT_DEVICE_DRIVER_CREATOR_GUID",

            "00000000-0000-0000-0000-000000000000" =>
                "CPER_EMPTY_GUID",

            _ => $"{id} - UNKNOWN GUID"
        };
    }
    public static string GetSectionType(Guid sectionType)
    {
        string section = sectionType.ToString();
        return section switch
        {
            "9876ccad-47b4-4bdb-b65e-16f193c4f3db" =>
                "PROCESSOR_GENERIC_ERROR_SECTION_GUID",

            "dc3ea0b0-a144-4797-b95b-53fa242b6e1d" =>
                "XPF_PROCESSOR_ERROR_SECTION_GUID",

            "e429faf1-3cb7-11d4-bca7-0080c73c8881" =>
               "IPF_PROCESSOR_ERROR_SECTION_GUID",

            "e19e3d16-bc11-11e4-9caa-c2051d5d46b0" =>
               "ARM_PROCESSOR_ERROR_SECTION_GUID",

            "a5bc1114-6f64-4ede-b863-3e83ed7c83b1" =>
               "MEMORY_ERROR_SECTION_GUID",

            "d995e954-bbc1-430f-ad91-b44dcb3c6f35" =>
               "PCIEXPRESS_ERROR_SECTION_GUID",

            "c5753963-3b84-4095-bf78-eddad3f9c9dd" =>
               "PCIXBUS_ERROR_SECTION_GUID",

            "eb5e4685-ca66-4769-b6a2-26068b001326" =>
               "PCIXDEVICE_ERROR_SECTION_GUID",

            "81212a96-09ed-4996-9471-8d729c8e69ed" =>
               "FIRMWARE_ERROR_RECORD_REFERENCE_GUID",

            "81687003-dbfd-4728-9ffd-f0904f97597d" =>
               "PMEM_ERROR_SECTION_GUID",

            "85183a8b-9c41-429c-939c-5c3c087ca280" =>
               "MU_TELEMETRY_SECTION_GUID",

            "c34832a1-02c3-4c52-a9f1-9f1d5d7723fc" =>
               "RECOVERY_INFO_SECTION_GUID",

            "00000000-0000-0000-0000-000000000000" =>
               "CPER_EMPTY_GUID",

            _ => $"{section} - UNKNOWN GUID"
        };
    }
    public static DateTime TranslateWheaTimestamp(ulong timestamp)
    {
        var bytes = BitConverter.GetBytes(timestamp);
        var second = bytes[0];
        var minute = bytes[1];
        var hour = bytes[2];
        // Ignore bytes[3] - It contains the "precise" bit, which is not important.
        var day = bytes[4];
        var month = bytes[5];
        var year = bytes[6];
        var century = bytes[7];
        int fullYear = century * 100 + year;
        return new DateTime(fullYear, month, day, hour, minute, second);
    }
    // I don't like this
    public static bool CheckWheaErrorValidity(WheaErrorHeader header, byte[] wheaByteArray, out string signature, out string revision, out string signatureEnd, out string length)
    {
        StringBuilder sig = new();
        StringBuilder rev = new();
        StringBuilder sigEnd = new();
        StringBuilder len = new();

        bool valid = true;

        sig.Append(header.Signature.ToString("X"));
        rev.Append(header.Revision.ToString("X"));
        sigEnd.Append(header.SignatureEnd.ToString("X"));
        len.Append(header.Length.ToString("X"));

        if (header.Signature == 0x52455043)
        {
            sig.Append(" -- VALID");
        }
        else
        {
            sig.Append(" -- INVALID");
            valid = false;
        }
        if (header.Revision == 0x0210)
        {
            rev.Append(" -- VALID");
        }
        else
        {
            rev.Append(" -- INVALID");
            valid = false;
        }
        if (header.SignatureEnd == 0xFFFFFFFF)
        {
            sigEnd.Append(" -- VALID");
        }
        else
        {
            sigEnd.Append(" -- INVALID");
            valid = false;
        }
        if (header.Length == wheaByteArray.Length)
        {
            len.Append(" -- VALID");
        }
        else
        {
            len.Append(" -- INVALID");
            valid = false;
        }

        signature = sig.ToString();
        revision = rev.ToString();
        signatureEnd = sigEnd.ToString();
        length = len.ToString();
        return valid;
    }
    public static List<WheaErrorDescriptorReadable> MakeReadableErrorDescriptors(List<WheaErrorDescriptor> descriptors)
    {
        var readableErrorDescriptors = new List<WheaErrorDescriptorReadable>();

        foreach (var descriptor in descriptors)
        {
            var readableDescriptor = new WheaErrorDescriptorReadable();

            readableDescriptor.SectionOffset = $"0x{descriptor.SectionOffset:X8}";
            readableDescriptor.SectionLength = $"0x{descriptor.SectionLength:X8}";
            readableDescriptor.Revision = $"0x{descriptor.Revision:X4}";
            readableDescriptor.ValidBits = $"0x{descriptor.ValidBits:X2}"; // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_whea_error_record_section_descriptor_validbits
            readableDescriptor.Flags = $"0x{descriptor.Flags:X8}"; // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_whea_error_record_section_descriptor

            // Translate GUID
            readableDescriptor.SectionType = GetSectionType(descriptor.SectionType);

            readableDescriptor.FRUId = descriptor.FRUId.ToString();
            readableDescriptor.SectionSeverity = descriptor.SectionSeverity.ToString();

            unsafe
            {
                readableDescriptor.FRUText = Marshal.PtrToStringAnsi((IntPtr)descriptor.FRUText);
            }

            readableErrorDescriptors.Add(readableDescriptor);
        }

        return readableErrorDescriptors;
    }
    public static WheaErrorDescriptor GetWheaDescriptor(byte[] WheaByteArray, int index)
    {
        var descriptorSize = Marshal.SizeOf(typeof(WheaErrorDescriptor));
        var offset = index * descriptorSize;
        byte[] descriptorArray = new byte[descriptorSize];
        var WheaHeaderEnd = Marshal.SizeOf(typeof(WheaErrorHeader));

        // Subsect the WHEA byte array to get just the bytes of the descriptor.
        Array.Copy(WheaByteArray, WheaHeaderEnd + offset, descriptorArray, 0, descriptorSize);

        WheaErrorDescriptor descriptor = WheaErrorDescriptor.FromBytes(descriptorArray);
        return descriptor;
    }
    public static string GetWheaErrorPacket(byte[] WheaByteArray, WheaErrorDescriptor descriptor)
    {
        StringBuilder errorPacket = new();
        var packetStart = descriptor.SectionOffset;
        var packetLength = descriptor.SectionLength;

        byte[] packetArray = new byte[packetLength];

        // Subsect the WHEA byte array to get just the bytes of the error packet.
        Array.Copy(WheaByteArray, packetStart, packetArray, 0, packetLength);

        string errorPacketBytes = BitConverter.ToString(packetArray).ToLower();
        errorPacketBytes = errorPacketBytes.Replace("-", " ");
        string errorPacketAscii = HexToAscii(errorPacketBytes);
        for (int i = 0; i < errorPacketBytes.Length; i++)
        {
            char c = errorPacketBytes[i];
            errorPacket.Append(errorPacketBytes[i]);
            if ((i + 1) % 48 == 0 && i != 0)
            {
                string hexOffset = (((i + 1) / 3) - 16).ToString("X").PadRight(2, '0');
                errorPacket.Append($" - 0x{hexOffset} - ");
                var asciiOffset = (i - 47) / 3;
                errorPacket.Append(errorPacketAscii.Substring(asciiOffset, 16));
                errorPacket.Append('\n');
            }
        }
        return errorPacket.ToString();
    }

    public static byte[] ByteArrayFromHexString(string str)
    {
        if (str.Length % 2 != 0)
        {
            LogEvent($"WHEA Error Record \"RawData\" string is invalid. Length: {str.Length}", Region.Events, EventType.ERROR);
            return null;
        }
        List<byte> bytes = new List<byte>();
        for (int i = 0; i < str.Length; i += 2)
        {
            var substr = str.Substring(i, 2);
            byte b = 0;
            if (!byte.TryParse(substr, System.Globalization.NumberStyles.HexNumber, null, out b))
            {
                LogEvent($"WHEA Error Record \"RawData\" string is invalid. Byte Parse Failure @ Index {i} - {substr}", Region.Events, EventType.ERROR);
                return null;
            }
            bytes.Add(b);
        }
        return bytes.ToArray();
    }
    public static XmlNode GetDataNode(EventRecord eventInstance)
    {
        string xmlString = eventInstance.ToXml();
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);

        XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        namespaceManager.AddNamespace("ns", "http://schemas.microsoft.com/win/2004/08/events/event");
        return xmlDoc.SelectSingleNode("/ns:Event/ns:EventData", namespaceManager);
    }
    /// <summary>
    /// Converts bytes into their ascii equivalent. Hex strings must have a separator between bytes.
    /// i.e. 0123456789ab is not valid. 01 23 45 67 89 ab is valid.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string HexToAscii(string str)
    {
        StringBuilder sb = new();
        for (int i = 0; i < str.Length; i += 3)
        {
            string hs = str.Substring(i, 2);
            var c = Convert.ToChar(Convert.ToUInt32(hs, 16));
            if (!char.IsControl(c))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('.');
            }
        }
        return sb.ToString();
    }
    public static bool CheckBitMask(ulong value, ulong mask)
    {
        return (value & mask) == mask;
    }
}