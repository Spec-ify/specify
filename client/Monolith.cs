using System;
using System.Management;
using System.Net;
using Newtonsoft.Json;

namespace specify_client;


/**
 * The big structure of all the things.
 * We will probably not make a Create() method here, because we want to get feedback about the process of creating
 *  the children
 */
public class Monolith
{
    public MonolithBasicInfo BasicInfo;

    /**
     * Debating making this static, because I don't like OOP
     */
    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}

public struct MonolithBasicInfo
{
    public string Edition;
    public string Version;
    public string InstallDate;
    public string Uptime;
    public string Hostname;
    public string Domain;
    public string BootMode;
    public string BootState;

    public static MonolithBasicInfo Create()
    {
        var os = DataCache.Os;
        var cs = DataCache.Cs;

        return new MonolithBasicInfo
        {
            Edition = (string) os["Caption"],
            Version = (string) os["Version"],
            InstallDate = Data.CimToIsoDate((string) os["InstallDate"]),
            Uptime = (DateTime.Now - ManagementDateTimeConverter.ToDateTime((string) os["LastBootUpTime"])).ToString("g"),
            Hostname = Dns.GetHostName(),
            Domain = Environment.GetEnvironmentVariable("userdomain"),
            BootMode = Environment.GetEnvironmentVariable("firmware_type"),
            BootState = (string) cs["BootupState"]
        };
    }
}
