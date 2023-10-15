using System;
using System.Linq;

namespace specify_client.data;

public static partial class Cache
{
    public static async System.Threading.Tasks.Task MakeMainData()
    {
        try
        {
            DebugLog.Region region = DebugLog.Region.Main;
            await DebugLog.StartRegion(region);
            Os = Utils.GetWmi("Win32_OperatingSystem").First();
            Cs = Utils.GetWmi("Win32_ComputerSystem").First();
            await DebugLog.LogEventAsync("Main WMI Data retrieved.", region);
            await DebugLog.EndRegion(region);
        }
        catch (Exception ex)
        {
            await DebugLog.LogFatalError($"{ex}", DebugLog.Region.Main);
        }
        MainDataWriteSuccess = true;
    }
}