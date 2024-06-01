using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

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

            // Make this whole section async
            await Utils.DoTask(DebugLog.Region.Security, "GetExclusionList", GetExclusionList);

            FwList = Utils.GetWmi("FirewallProduct", "displayName", @"root\SecurityCenter2")
                .Select(x => (string)x["displayName"]).ToList();
            await DebugLog.LogEventAsync("Security WMI Information Retrieved.", region);

            if (Environment.GetEnvironmentVariable("firmware_type")!.Equals("UEFI"))
            {
                var secBootEnabled = Utils.GetRegistryValue<int?>(
                    Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SecureBoot\State",
                    "UEFISecureBootEnabled");

                if (secBootEnabled == null) await DebugLog.LogEventAsync($"Could not get UEFISecureBootEnabled value", region, DebugLog.EventType.ERROR);
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
                await DebugLog.LogEventAsync("Security Data: could not get TPM. This is probably because specify was not run as administrator.", region, DebugLog.EventType.WARNING);
            }
            await DebugLog.LogEventAsync("TPM Information Retrieved.", region);

            UacLevel = Utils.GetRegistryValue<int?>(
                Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                "ConsentPromptBehaviorUser");
            var enableLua = Utils.GetRegistryValue<int?>(Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA");
            if (enableLua == null) await DebugLog.LogEventAsync($"Security data: could not get EnableLUA value", region, DebugLog.EventType.ERROR);
            else UacEnabled = enableLua == 1;
            await DebugLog.LogEventAsync("UAC Information retrieved.", region);

            await DebugLog.EndRegion(DebugLog.Region.Security);
        }
        catch (Exception ex)
        {
            await DebugLog.LogFatalError($"{ex}", DebugLog.Region.Security);
        }
        SecurityWriteSuccess = true;
    }
    private static async Task GetExclusionList()
    {
        Dictionary<string, object> exclusions;

        try
        {
            exclusions = Utils.GetWmi("MSFT_MpPreference", "*", @"root\Microsoft\Windows\Defender").FirstOrDefault();
        }
        catch (ManagementException ex)
        {
            await DebugLog.LogEventAsync($"Could not retrieve exclusions. {ex.Message}", DebugLog.Region.Security, DebugLog.EventType.WARNING);
            return;
        }

        Utils.TryWmiRead(exclusions, "ExclusionPath", out string[] exclusionPath);
        Utils.TryWmiRead(exclusions, "ExclusionExtension", out string[] exclusionExtension);
        Utils.TryWmiRead(exclusions, "ExclusionProcess", out string[] exclusionProcess);
        Utils.TryWmiRead(exclusions, "ExclusionIpAddress", out string[] exclusionIpAddresses);

        // We cannot error log this as the keys simply do not exist in WMI if no exclusions are set.

        if (exclusionPath != null)
        {
            ExclusionPath = exclusionPath.ToList();
        }
        else
        {
            ExclusionPath = new();
        }

        if (exclusionExtension != null)
        {
            ExclusionExtension = exclusionExtension.ToList();
        }
        else
        {
            ExclusionExtension = new();
        }

        if (exclusionProcess != null)
        {
            ExclusionProcess = exclusionProcess.ToList();
        }
        else
        {
            ExclusionProcess = new();
        }

        if (exclusionIpAddresses != null)
        {
            ExclusionIpAddresses = exclusionIpAddresses.ToList();
        }
        else
        {
            ExclusionIpAddresses = new();
        }

    }
    public static List<string> AVList()
    {
        var antiviruses = Utils.GetWmi("AntivirusProduct", "displayName", @"root\SecurityCenter2")
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
            antiviruses.RemoveAll(x => x == "Windows Defender");
            antiviruses.Add("Windows Defender (Disabled)");
        }

        // Same, but checks in policies
        else if (PassiveModePolicies != 0 || DisableAVPolicies != 0 || DisableASWPolicies != 0)
        {
            antiviruses.RemoveAll(x => x == "Windows Defender");
            antiviruses.Add("Windows Defender (Disabled)");
        }

        // Check if Defender is not the only entry in list
        else if (antiviruses.Count > 1 && antiviruses.All(a => a == "Windows Defender"))
        {
            antiviruses.RemoveAll(x => x == "Windows Defender");
            antiviruses.Add("Windows Defender");
        }

        return antiviruses;
    }
}