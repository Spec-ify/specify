// From https://github.com/jlnewton87/Programming/tree/master/C%23/Windows%20API%20Code%20Pack%201.1/source/WindowsAPICodePack/Core/Interop/NetworkList

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace specify_client.Data.Methods.NLM
{
    [ComImport, Guid("DCB00005-570F-4A9B-8D69-199FDBA5723B")]
    internal interface INetworkConnection
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        INetwork GetNetwork();

        bool IsConnectedToInternet { get; }

        bool IsConnected { get; }

        ConnectivityStates GetConnectivity();

        Guid GetConnectionId();

        Guid GetAdapterId();

        DomainType GetDomainType();
    }

    [ComImport, Guid("DCB00002-570F-4A9B-8D69-199FDBA5723B")]
    internal interface INetwork
    {
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetName();

        void SetName([In, MarshalAs(UnmanagedType.BStr)] string szNetworkNewName);

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetDescription();

        void SetDescription([In, MarshalAs(UnmanagedType.BStr)] string szDescription);

        Guid GetNetworkId();

        DomainType GetDomainType();

        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumerable GetNetworkConnections();

        void GetTimeCreatedAndConnected(
            out uint pdwLowDateTimeCreated,
            out uint pdwHighDateTimeCreated,
            out uint pdwLowDateTimeConnected,
            out uint pdwHighDateTimeConnected);

        bool IsConnectedToInternet { get; }

        bool IsConnected { get; }

        ConnectivityStates GetConnectivity();

        NetworkCategory GetCategory();

        void SetCategory([In] NetworkCategory NewCategory);
    }

    [ComImport, Guid("DCB00000-570F-4A9B-8D69-199FDBA5723B"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface INetworkListManager
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        [DispId(3)]
        IEnumerable GetNetworkConnections();
    }
}
