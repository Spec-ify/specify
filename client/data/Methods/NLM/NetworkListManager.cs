using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace specify_client.Data.Methods.NLM
{
    [ComImport, Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B")]
    public class NetworkListManager { }

    public class NlmClass
    {
        public List<NlmConnectionStatus> GetNlmConnectionStatus()
        {
            List<NlmConnectionStatus> profiles = new();
            INetworkListManager nlm = (INetworkListManager)new NetworkListManager();

            IEnumerable netConnections = nlm.GetNetworkConnections();

            foreach (INetworkConnection netConnection in netConnections)
            {
                INetwork network = netConnection.GetNetwork();

                NlmConnectionStatus profile = new ();
                profile.NetworkAdapterId = netConnection.GetAdapterId();
                profile.Connectivity = netConnection.GetConnectivity().ToString();
                profile.IsConnectedToInternet = netConnection.IsConnectedToInternet;
                profile.Category = network.GetCategory().ToString();

                profiles.Add(profile);
            }

            return profiles;
        }

    }

    public class NlmConnectionStatus()
    {
        public string Category { get; set; }
        public string Connectivity { get; set; }
        public bool IsConnectedToInternet { get; set; }

        [JsonIgnore]
        public Guid NetworkAdapterId { get; set; }
    }
}
