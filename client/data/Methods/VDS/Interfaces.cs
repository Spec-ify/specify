using System;
using System.Runtime.InteropServices;

namespace specify_client.Data.Methods.VDS
{
    [ComImport, Guid("E0393303-90D4-4A97-AB71-E9B671EE2729"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsServiceLoader
    {
        [PreserveSig] int LoadService([In][MarshalAs(UnmanagedType.LPWStr)] string machineName, [Out] out IVdsService service);
    }

    [ComImport, Guid("0818A8EF-9BA9-40D8-A6F9-E22833CC771E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsService
    {
        void IsServiceReady();
        [PreserveSig] int WaitForServiceReady();
        void GetProperties();
        [PreserveSig] int QueryProviders([In] VDS_QUERY_PROVIDER_FLAG masks, [Out] out IEnumVdsObject ppEnum);
        void QueryMaskedDisks();
        void QueryUnallocatedDisks(out IEnumVdsObject ppEnum);
        void GetObject();
        [PreserveSig] int QueryDriveLetters([In]ushort wcFirstLetter, [In] int count, [In, Out, MarshalAs(UnmanagedType.LPArray,SizeParamIndex = 1)] VDS_DRIVE_LETTER_PROP[] array);
    }

    [ComImport, Guid("118610B7-8D94-4030-B5B8-500889788E4E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumVdsObject
    {
        [PreserveSig] int Next([In] uint celt, [MarshalAs(UnmanagedType.IUnknown)][Out] out object ppObjectArray, [Out] out int pcFetched);
    }

    [ComImport, Guid("3b69d7f5-9d94-4648-91ca-79939ba263bf"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsPack
    {
        void GetProperties();
        void GetProvider();
        [PreserveSig] int QueryVolumes(out IEnumVdsObject volumesEnumerator);
        [PreserveSig] int QueryDisks(out IEnumVdsObject disksEnumerator);

    }

    [ComImport, Guid("07e5c822-f00c-47a1-8fce-b244da56fd06"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsDisk
    {
        [PreserveSig]
        int GetProperties(out VDS_DISK_PROP properties);
        void GetPack();
        void GetIdentificationData();

        [PreserveSig]
        int QueryExtents([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out VDS_DISK_EXTENT[] ppExtentArray, out int plNumberOfExtents);
    }

    [ComImport, Guid("10C5E575-7984-4E81-A56B-431F5F92AE42"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsProvider
    {
    }

    [ComImport, Guid("9aa58360-ce33-4f92-b658-ed24b14425b8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsSwProvider
    {
        [PreserveSig]
        int QueryPacks(out IEnumVdsObject packEnumerator);

        [PreserveSig]
        int CreatePack(out IVdsPack pack);
    }

    [ComImport, Guid("88306bb2-e71f-478c-86a2-79da200a0f11"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsVolume
    {
        [PreserveSig]
        int GetProperties(out VDS_VOLUME_PROP properties);

        [PreserveSig]
        int GetPack(out IVdsPack pack);

        [PreserveSig]
        int QueryPlexes(out IEnumVdsObject plexEnumerator);
    }

    [ComImport, Guid("EE2D5DED-6236-4169-931D-B9778CE03DC6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsVolumeMF
    {
        [PreserveSig]
        int GetFileSystemProperties(out VDS_FILE_SYSTEM_PROP fileSystemProp);
    }

    [ComImport, Guid("4DAA0135-E1D1-40F1-AAA5-3CC1E53221C3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsVolumePlex
    {
        void GetProperties();
        void GetVolume();
        [PreserveSig] int QueryExtents([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out VDS_DISK_EXTENT[] extents, out int numberOfExtents);
        void Repair();
    }

    [ComImport, Guid("3858C0D5-0F35-4BF5-9714-69874963BC36"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVdsAdvancedDisk3
    {
        [PreserveSig] int GetProperties(out VDS_ADVANCEDDISK_PROP advDiskprop);
    }
}
