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
            /*await DebugLog.LogEventAsync("UNEXPECTED FATAL EXCEPTION", DebugLog.Region.Security, DebugLog.EventType.ERROR);
            await DebugLog.LogEventAsync($"{ex}", DebugLog.Region.Security);
            Environment.Exit(-1);*/
            await DebugLog.LogFatalError($"{ex}", DebugLog.Region.Security);
        }
    }

    public static List<string> AVList()
    {
        var antiviruses = new List<string>();

        antiviruses = Utils.GetWmi("AntivirusProduct", "displayName", @"root\SecurityCenter2")
                            .Select(x => (string)x["displayName"]).ToList();

        // Checks for registry items
        int PassiveMode = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender",
                "PassiveMode") ?? 0;

        int DisableAV = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender",
                "DisableAntiVirus") ?? 0;

        int DisableASW = Utils.GetRegistryValue<int?>(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender",
                "DisableAntiSpyware") ?? 0;

        int PassiveModePolicies = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender",
                "PassiveMode") ?? 0;

        int DisableAVPolicies = Utils.GetRegistryValue<int?>(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender",
                "DisableAntiVirus") ?? 0;

        int DisableASWPolicies = Utils.GetRegistryValue<int?>(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender",
                "DisableAntiSpyware") ?? 0;

        // Move to end of list
        // Check if Defender is disabled in any way
        if (PassiveMode != 0 || DisableAV != 0 || DisableASW != 0)
        {
            antiviruses.RemoveAll(x => ((string)x) == "Windows Defender");
            antiviruses.Add("Windows Defender (Disabled)");
        }

        // Same, but checks in policies
        else if (PassiveModePolicies != 0 || DisableAVPolicies != 0 || DisableASWPolicies != 0)
        {
            antiviruses.RemoveAll(x => ((string)x) == "Windows Defender");
            antiviruses.Add("Windows Defender (Disabled)");
        }

        // Check if Defender is not the only entry in list
        else if (antiviruses.Count > 1 && antiviruses.All(a => a == "Windows Defender"))
        {
            antiviruses.RemoveAll(x => ((string)x) == "Windows Defender");
            antiviruses.Add("Windows Defender");
        }

        return antiviruses;
    }
}