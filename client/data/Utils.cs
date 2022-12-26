using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;

namespace specify_client.data;

/**
 * <summary>
 * Collection of utility functions for data gathering
 * </summary>
 */
public class Utils
{
    /**
     * <summary>
     * Gets the WMI object (with GetWmiObj), and converts it to a dictionary.
     * </summary>
     * <seealso cref="GetWmiObj"/>
     */
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
        return (T)value;
    }
    public static Browser.Extension ParseChromiumExtension(string path)
    {
        try
        {
            string ldir = string.Concat(Directory.GetDirectories(path).Last(), "\\_locales\\");
            JToken localeData = JObject.Parse("{}"); //Prevents NullReferenceException when locale does not exist
            ChromiumManifest manifest = JsonConvert.DeserializeObject<ChromiumManifest>(
                File.ReadAllText(string.Concat(Directory.GetDirectories(path).Last(), "\\manifest.json")));

            if (Regex.IsMatch(manifest.name, "MSG_(.+)") || Regex.IsMatch(manifest.description, "MSG_(.+)"))
                localeData = JObject.Parse(File.ReadAllText(string.Concat(ldir, manifest.default_locale, "\\messages.json")));
            try
            {
                return new Browser.Extension()
                {
                    name = (Regex.IsMatch(manifest.name, "MSG_(.+)"))
                    ? (string)localeData[manifest.name.Substring(6, manifest.name.Length - 8)]["message"] : manifest.name,
                    description = (Regex.IsMatch(manifest.description, "MSG_(.+)"))
                    ? (string)localeData[manifest.description.Substring(6, manifest.description.Length - 8)]["message"] : manifest.description,
                    version = manifest.version
                };
            } 
            catch(NullReferenceException) 
            {
                /*
                 * This handles a rare issues with the format between the manifest and locale
                 * Essentially the contextual code in manifest can be all caps while the corresponding field in messages is not.
                 * This mismatch created a null reference. Adding ToLower() in a normal context breaks a lot of extensions for reading.
                 * If you've got a cleaner way of doing this, feel free to do it.
                */
                return new Browser.Extension()
                {
                    name = (Regex.IsMatch(manifest.name, "MSG_(.+)"))
                    ? (string)localeData[manifest.name.Substring(6, manifest.name.Length - 8).ToLower()]["message"] : manifest.name,
                    description = (Regex.IsMatch(manifest.description, "MSG_(.+)"))
                    ? (string)localeData[manifest.description.Substring(6, manifest.description.Length - 8).ToLower()]["message"] : manifest.description,
                    version = manifest.version
                };
            }
        }
        catch (Exception e)
        {
            if (e is FileNotFoundException || e is JsonException)
                throw e;
            else
                return null;
        }
    }
}
