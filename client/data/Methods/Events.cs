using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace specify_client.data;

using static specify_client.DebugLog;
using static Utils;

public static partial class Cache
{
    public static async Task MakeEventData()
    {
        try
        {
            List<Task> eventTaskList = new();
            Region region = Region.Events;
            await StartRegion(region);

            eventTaskList.Add(GetUnexpectedShutdowns());

            await Task.WhenAll(eventTaskList);
            await EndRegion(region);
        }
        catch (Exception ex)
        {
            await LogFatalError($"{ex}", Region.Events);
        }
        /*
         * var WheaHeader = WheaErrorHeader.FromBytes(WheaByteArray);

            Console.WriteLine(WheaHeader.SignatureEnd);
            Console.WriteLine(WheaHeader.CreatorId);
            Console.WriteLine(WheaHeader.NotifyType);
            Console.WriteLine(WheaHeader.Severity);

            var WheaHeaderEnd = Marshal.SizeOf(typeof(WheaErrorHeader));

            for(int i = 0; i < WheaHeader.SectionCount; i++)
            {
                var offset = i * Marshal.SizeOf(typeof(WheaErrorDescriptor));
                WheaErrorDescriptor descriptor = WheaErrorDescriptor.FromBytes(WheaByteArray[(WheaHeaderEnd + offset)..]);
                Console.WriteLine(descriptor.SectionType);
                Console.WriteLine($"PACKET: {descriptor.SectionOffset} - {descriptor.SectionOffset + descriptor.SectionLength}");
                Console.WriteLine(BitConverter.ToString(WheaByteArray[(int)descriptor.SectionOffset..(int)(descriptor.SectionOffset + descriptor.SectionLength)]));
            }*/
    }
    public static async Task GetUnexpectedShutdowns()
    {
        string TaskName = "GetUnexpectedShutdowns";
        await OpenTask(Region.Events, TaskName);

        string eventLogName = "System";
        int targetEventId = 41;
        string query = $"*[System[EventID={targetEventId}]]";

        using (EventLogReader logReader = new EventLogReader(new EventLogQuery(eventLogName, PathType.LogName, query)))
        {
            int maxEvents = 10; // I don't think we need more than 10. The cursed powershell command only grabs 10.
            UnexpectedShutdowns = new();

            for (EventRecord eventInstance = logReader.ReadEvent(); eventInstance != null; eventInstance = logReader.ReadEvent())
            {
                if(UnexpectedShutdowns.Count >= maxEvents)
                {
                    break;
                }

                UnexpectedShutdown shutdown = new();

                shutdown.Timestamp = eventInstance.TimeCreated;

                string xmlString = eventInstance.ToXml();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlString);

                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
                namespaceManager.AddNamespace("ns", "http://schemas.microsoft.com/win/2004/08/events/event");
                XmlNode eventDataNode = xmlDoc.SelectSingleNode("/ns:Event/ns:EventData", namespaceManager);

                if (eventDataNode != null)
                {
                    foreach (XmlNode dataNode in eventDataNode.ChildNodes)
                    {
                        if (dataNode.Name == "Data")
                        {
                            string dataName = dataNode.Attributes["Name"].Value;
                            string dataValue = dataNode.InnerText;
                            try
                            {

                                if (dataName.Equals("BugcheckCode"))
                                {
                                    int.TryParse(dataValue, out shutdown.BugcheckCode);
                                }
                                if (dataName.Equals("BugcheckParameter1"))
                                {
                                    shutdown.BugcheckParameter1 = Convert.ToUInt64(dataValue, 16);
                                }
                                if (dataName.Equals("BugcheckParameter2"))
                                {
                                    shutdown.BugcheckParameter2 = Convert.ToUInt64(dataValue, 16);
                                }
                                if (dataName.Equals("BugcheckParameter3"))
                                {
                                    shutdown.BugcheckParameter3 = Convert.ToUInt64(dataValue, 16);
                                }
                                if (dataName.Equals("BugcheckParameter4"))
                                {
                                    shutdown.BugcheckParameter4 = Convert.ToUInt64(dataValue, 16);
                                }
                                if (dataName.Equals("PowerButtonTimestamp"))
                                {
                                    ulong.TryParse(dataValue, out shutdown.PowerButtonTimestamp);
                                }
                            }
                            catch (Exception ex)
                            {
                                await LogEventAsync($"Data parsing error in GetUnexpectedShutdowns {ex}", Region.Events, EventType.ERROR);
                            }
                        }
                    }
                }
                UnexpectedShutdowns.Add(shutdown);
            }
        }
        await CloseTask(Region.Events, TaskName);
    }

}
