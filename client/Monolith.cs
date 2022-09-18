using System.Management;
using System.Net;

namespace specify_client;


/**
 * The big structure of all the things
 */
public class Monolith
{
    public MonolithBasicInfo BasicInfo;
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
        var os = Data.GetWmi("Win32_OperatingSystem").First();
        var cs = Data.GetWmi("Win32_ComputerSystem").First();

        return new MonolithBasicInfo
        {
            Edition = (string) os["Caption"],
            Version = (string) os["Version"],
            InstallDate = Data.CimToISODate((string) os["InstallDate"]),
            Uptime = (DateTime.Now - ManagementDateTimeConverter.ToDateTime((string) os["InstallDate"])).ToString("g"),
            Hostname = Dns.GetHostName(),
            Domain = Environment.GetEnvironmentVariable("userdomain"),
            BootMode = Environment.GetEnvironmentVariable("firmware_type"),
            BootState = (string) cs["BootupState"]
        };
    }
}
