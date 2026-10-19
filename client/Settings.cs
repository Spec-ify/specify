namespace specify_client;

public static class Settings
{
    /// <summary>
    /// Redacts Windows username and OneDrive commercial account name
    /// </summary>
    public static bool RedactUsername { get; set; } = false;
    public static bool RedactSerialNumber { get; set; } = false;
    public static bool RedactNetworking { get; set; } = false;
    public static bool DontUpload { get; set; } = false;
    public static bool Headless { get; set; } = false;
    public static bool LocalCulture { get; set; } = false;
    public static bool EnableDebug = false; // Deliberately not a property.
}