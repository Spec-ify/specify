namespace specify_client;

public static class Settings
{
    public static bool RedactUsername { get; set; } = false;
    public static bool RedactOneDriveCommercial { get; set; } = false;
    public static bool DontUpload { get; set; } = false;
    public static bool DisableDebug = false; // Deliberately not a property.
}