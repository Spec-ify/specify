using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace specify_client.data;

public static partial class Cache
{
    public static async System.Threading.Tasks.Task MakeSecurityData()
    {
        try
        {
            DebugLog.Region region = DebugLog.Region.Security;
            await DebugLog.StartRegion(region);
            AvList = AVList();
            FwList = Utils.GetWmi("FirewallProduct", "displayName", @"root\SecurityCenter2")
                .Select(x => (string)x["displayName"]).ToList();
            await DebugLog.LogEventAsync("Security WMI Information Retrieved.", region);



            if (Environment.GetEnvironmentVariable("firmware_type")!.Equals("UEFI"))
            {
                var secBootEnabled = Utils.GetRegistryValue<int?>(
                    Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SecureBoot\State",
                    "UEFISecureBootEnabled");

                if (secBootEnabled == null) Issues.Add($"Security data: could not get UEFISecureBootEnabled value");
                else SecureBootEnabled = secBootEnabled == 1;
            }
            await DebugLog.LogEventAsync("SecureBoot Information Retrieved.", region);

            try
            {
                Tpm = Utils.GetWmi("Win32_Tpm", "*", @"Root\CIMV2\Security\MicrosoftTpm").First();
                Tpm["IsPresent"] = true;
            }
            catch (InvalidOperationException)
            {
                // No TPM
                Tpm = new Dictionary<string, object>() { { "IsPresent", false } };
            }
            catch (ManagementException)
            {
                Tpm = null;
                Issues.Add("Security Data: could not get TPM. This is probably because specify was not run as administrator.");
            }
            await DebugLog.LogEventAsync("TPM Information Retrieved.", region);

            UacLevel = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                "ConsentPromptBehaviorUser");
            var enableLua = Utils.GetRegistryValue<int?>(Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA");
            if (enableLua == null) Issues.Add($"Security data: could not get EnableLUA value");
            else UacEnabled = enableLua == 1;
            await DebugLog.LogEventAsync("UAC Information retrieved.", region);

            await DebugLog.EndRegion(DebugLog.Region.Security);
        }
        catch (Exception ex)
        {
            await DebugLog.LogEventAsync("UNEXPECTED FATAL EXCEPTION", DebugLog.Region.Security, DebugLog.EventType.ERROR);
            await DebugLog.LogEventAsync($"{ex}", DebugLog.Region.Security);
            Monolith.ProgramDone(3);
        }
    }
    public static List<string> AVList()
    {

        var antiviruses = new List<string>();

        antiviruses = Utils.GetWmi("AntivirusProduct", "displayName", @"root\SecurityCenter2")
                            .Select(x => (string)x["displayName"]).ToList();

        int? PassiveMode = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender",
                "PassiveMode");

        int? DisableAV = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender",
                "DisableAntiVirus");

        int? DisableASW = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender",
                "DisableAntiSpyware");

        if (PassiveMode != null || DisableAV != 0 || DisableASW != 0)
        {
            antiviruses.RemoveAll(x => ((string)x) == "Windows Defender");
        }

        return antiviruses;
    }
}