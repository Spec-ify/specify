using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static specify_client.DebugLog;

namespace specify_client.data;

/**
 * <summary>
 * Collection of utility functions for data gathering
 * </summary>
 */

public static class Utils
{
    /**
     * <summary>
     * Gets the WMI object (with GetWmiObj), and converts it to a dictionary.
     * </summary>
     * <seealso cref="GetWmiObj"/>
     */
    public enum AutoTuningLevels
    {
        Disabled,
        HighlyRestricted,
        Restricted,
        Normal,
        Experimental
    }
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
     * <p>Convert a CIM date (what would be gotten from WMI) into an ISO date</p>
     * <p><a href="https://learn.microsoft.com/en-us/windows/win32/wmisdk/cim-datetime">
     *      CIM DateTime on learn.microsoft.com
     * </a></p>
     * </summary>
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
        try
        {
            return (T)value;
        }
        catch (InvalidCastException)
        {
            var msg = $"Registry item {regKey.Name}\\{path}\\{name} cast to {nameof(T)} failed";
            DebugLog.LogEvent(msg, DebugLog.Region.System, DebugLog.EventType.ERROR);
            if(typeof(T).Equals(typeof(int?)))
            {
                // -1111 to make it very obvious this is a cast failure. Some keys are set to -1 by optimizers. Nothing uses -1111.
                return (T)(object)-1111;
            }
            if(typeof(T).Equals(typeof(string)))
            {
                return (T)(object)"Cast Failure";
            }
            return def;
        }
    }

    public static Browser.Extension ParseChromiumExtension(string path)
    {
        try
        {
            string ldir = string.Concat(Directory.GetDirectories(path).Last(), "\\_locales\\");
            JObject localeData = null;
            ChromiumManifest manifest = JsonConvert.DeserializeObject<ChromiumManifest>(
                File.ReadAllText(string.Concat(Directory.GetDirectories(path).Last(), "\\manifest.json")));

            bool manifestName = false;
            bool manifestDescription = false;

            if(!string.IsNullOrEmpty(manifest.name) && !string.IsNullOrEmpty(manifest.description))
            {
                if (Regex.IsMatch(manifest.name, "MSG_(.+)") || Regex.IsMatch(manifest.description, "MSG_(.+)"))
                {
                    localeData = JObject.Parse(File.ReadAllText(string.Concat(ldir, manifest.default_locale, "\\messages.json")));
                    manifestName = Regex.IsMatch(manifest.name, "MSG_(.+)");
                    manifestDescription = Regex.IsMatch(manifest.description, "MSG_(.+)");
                }
            }
            if(localeData is null)
            {
                localeData = JObject.Parse("{}"); //Prevents NullReferenceException when locale does not exist
            }
            string fallbackName = "";
            string fallbackDescription = "";

            // I think there's an issue with this substring command, more testing will be required.
            var jObj = localeData[manifest.name.Substring(6, manifest.name.Length - 8)];

            if (!(jObj is null))
            {
                fallbackName = (string)jObj["message"];
                if (string.IsNullOrEmpty(fallbackName))
                {
                    fallbackName = (string)jObj["message"];
                }

                fallbackDescription = (string)localeData[manifest.description.Substring(6, manifest.description.Length - 8)]["message"];
                if (string.IsNullOrEmpty(fallbackDescription))
                {
                    fallbackDescription = (string)localeData[manifest.description.Substring(6, manifest.description.Length - 8).ToLower()]["message"];
                }
            }
            return new Browser.Extension()
            {
                name = manifestName
                ? fallbackName : manifest.name,
                description = manifestDescription
                ? fallbackDescription : manifest.description,
                version = manifest.version
            };
        }
        catch (FileNotFoundException)
        {
            DebugLog.LogEvent($"Chromium extension files could not be found.", DebugLog.Region.System, DebugLog.EventType.WARNING);
            return null;
        }
        catch (JsonException)
        {
            DebugLog.LogEvent($"Chromium extension json files corrupt or invalid.", DebugLog.Region.System, DebugLog.EventType.WARNING);
            return null;
        }
        catch (ArgumentOutOfRangeException)
        {
            DebugLog.LogEvent($"Chromium extension path invalid: {path}", DebugLog.Region.System, DebugLog.EventType.WARNING);
            return null;
        }
        catch (Exception e)
        {
            DebugLog.LogEvent($"Unexpected exception occured in ParseChromiumExtension: {e}", DebugLog.Region.System, DebugLog.EventType.ERROR);
            return null;
        }
    }
    /// <summary>
    /// Attempts to retrieve a value from a WMI Dictionary retrieved through GetWmi(). The value will be stored in `value`.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>true if the value could be retrieved and is of the requested data type</returns>
    public static bool TryWmiRead<T>(this Dictionary<string, object> collection, string key, out T value)
    {
        bool success = collection.TryGetValue(key, out object? wmi) && wmi is T;
        value = success ? (T)wmi : default;
        return success;
    }
    public static async Task DoTask(Region region, string taskName, Func<Task> task)
    {
        await OpenTask(region, taskName);
        try
        {
            await Task.Run(task);
        }
        finally
        {
            await CloseTask(region, taskName);
        }
    }
    public static async Task DoTask(Region region, string taskName, Action task)
    {
        await OpenTask(region, taskName);
        try
        {
            await Task.Run(task);
        }
        finally
        {
            await CloseTask(region, taskName);
        }
    }
}