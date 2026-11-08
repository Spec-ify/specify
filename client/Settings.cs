namespace specify_client;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Settings
{
    public static Settings Instance { get; set; } = new Settings();

    /// <summary>
    /// Redacts Windows username and OneDrive commercial account name
    /// </summary>
    public bool RedactUsername { get; set; } = true;
    public bool RedactSerialNumber { get; set; } = false;
    public bool RedactNetworking { get; set; } = false;
    public bool RedactHostsFile { get; set; } = false;
    public bool DontUpload { get; set; } = false;
    public bool Headless { get; set; } = false;
    public bool LocalCulture { get; set; } = false;

    [JsonIgnore]
    public bool EnableDebug { get; set; } = false;
}