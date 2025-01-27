namespace specify_client;

public static class Settings
{
    public static bool RedactUsername { get; set; } = false;
    public static bool RedactSerialNumber { get; set; } = false;
    public static bool RedactOneDriveCommercial { get; set; } = false;
    public static bool DontUpload { get; set; } = false;
    public static bool LocalCulture { get; set; } = false;
    public static bool EnableDebug = false; // Deliberately not a property.
}