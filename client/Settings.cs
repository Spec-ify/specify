namespace specify_client;

public class Settings
{
    public static Settings Instance { get; set; } = new Settings();

    /// <summary>
    /// Redacts Windows username and OneDrive commercial account name
    /// </summary>
    public bool RedactUsername { get; set; } = false;
    public bool RedactSerialNumber { get; set; } = false;
    public bool RedactNetworking { get; set; } = false;
    public bool DontUpload { get; set; } = false;
    public bool Headless { get; set; } = false;
    public bool LocalCulture { get; set; } = false;
    public bool EnableDebug { get; set; } = false;
}