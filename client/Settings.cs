namespace specify_client;
using System.Reflection;
using System.Collections.Generic;

public static class Settings
{
    public static bool RedactUsername { get; set; } = true;
    public static bool RedactSerialNumber { get; set; } = false;
    public static bool RedactOneDriveCommercial { get; set; } = false;
    public static bool RedactNetworking { get; set; } = false;
    public static bool DontUpload { get; set; } = false;
    public static bool Headless { get; set; } = false;
    public static bool LocalCulture { get; set; } = false;
    public static bool EnableDebug = false; // Deliberately not a property.

    /** We cannot directly serialize a static class */
    public static Dictionary<string, bool> Serialize()
    {
        var res = new Dictionary<string, bool>();
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

        foreach (var property in typeof(Settings).GetProperties(flags))
            res[property.Name] = (bool)property.GetValue(null);
        foreach (var field in typeof(Settings).GetProperties(flags))
            res[field.Name] = (bool)field.GetValue(null);

        return res;
    }
}