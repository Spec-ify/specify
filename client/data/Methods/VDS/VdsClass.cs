using specify_client.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;
using static specify_client.DebugLog;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace specify_client.Data.Methods.VDS
{
    public static class VDSClass
    {
        //COM HResults
        public const uint HR_PROPERTIES_INCOMPLETE = 0x00042715u;

        private const int DRIVE_LETTER_COUNT = 26;
        public static IVdsService Create()
        {
            var vdsLoader = (IVdsServiceLoader)new VdsServiceLoader();
            int hr = vdsLoader.LoadService(null, out IVdsService vdsService);
            Marshal.ThrowExceptionForHR(hr);

            hr = vdsService.WaitForServiceReady();
            Marshal.ThrowExceptionForHR(hr);

            return vdsService;
        }

        public static List<DiskDrive> GetDisksInfo()
        {
            List<DiskDrive> drives = new();

            int hr;

            IVdsService vdsService = Create();

            VDS_DRIVE_LETTER_PROP[] driveLetters = new VDS_DRIVE_LETTER_PROP[DRIVE_LETTER_COUNT];

            var packs = GetPacks(vdsService);

            var vdsDisks = GetDisks(packs);
            var vdsVolumes = GetVolumes(packs);
            hr = vdsService.QueryDriveLetters('A', 26, driveLetters);
            if (!(hr == 0 || hr == HR_PROPERTIES_INCOMPLETE))
            {
                LogEvent($"Unable to get drive letters from VDS. Exception: {Marshal.GetExceptionForHR(hr).Message}", Region.Hardware, EventType.ERROR);
            }

            foreach (var vdsDisk in vdsDisks)
            {
                DiskDrive disk = new DiskDrive();

                hr = vdsDisk.GetProperties(out VDS_DISK_PROP diskProps);
                if (hr != 0 && hr != 1 && hr != HR_PROPERTIES_INCOMPLETE)
                {
                    LogEvent($"Unable to get drive data from VDS. Exception: {Marshal.GetExceptionForHR(hr).Message}", Region.Hardware, EventType.ERROR);
                    continue;
                }

                disk = new DiskDrive();
                disk.DiskCapacity = diskProps.Size;
                disk.DeviceName = string.Copy(diskProps.FriendlyName ?? string.Empty);
                disk.SerialNumber = string.Empty; // VDS_DISK_PROP doesnt expose serial number
                disk.PartitionScheme = Enum.GetName(typeof(VDS_PARTITION_STYLE), diskProps.PartitionStyle);
                disk.InstanceId = diskProps.DiskGuid.ToString();
                disk.InterfaceType = Enum.GetName(typeof(VDS_STORAGE_BUS_TYPE), diskProps.BusType);
                disk.MediaType = Enum.GetName(typeof(VDS_MEDIA_TYPE), diskProps.MediaType);
                disk.BlockSize = diskProps.BytesPerSector;
                disk.DiskNumber = (uint)drives.Count;
                disk.DeviceId = string.Copy(diskProps.Name ?? string.Empty);

                if (vdsDisk is IVdsAdvancedDisk3 advDisk3) // May not cast, if disk is a dynamic disk
                {
                    hr = advDisk3.GetProperties(out VDS_ADVANCEDDISK_PROP advDiskProps); // VDS_ADVANCEDDISK_PROP exposes more data than VDS_DISK_PROP
                    if (hr != 0 && hr != 1 && hr != HR_PROPERTIES_INCOMPLETE)
                    {
                        LogEvent($"Unable to get advanced drive data from VDS. Exception: {Marshal.GetExceptionForHR(hr).Message}", Region.Hardware, EventType.WARNING); // Warning, as it's only needed for serial number. For dynamic drives it's normal
                    }
                    else
                    {
                        disk.SerialNumber = string.Copy(advDiskProps.SerialNumber ?? string.Empty);
                    }
                }

                drives.Add(disk);

                hr = vdsDisk.QueryExtents(out VDS_DISK_EXTENT[] extents, out _);
                if (hr != 0 && hr != 1 && hr != HR_PROPERTIES_INCOMPLETE)
                {
                    LogEvent($"Unable to get advanced drive extend data from VDS for disk {disk.DeviceName}. Exception: {Marshal.GetExceptionForHR(hr).Message}", Region.Hardware, EventType.ERROR);
                    continue;
                }


                disk.Partitions = extents.Select(x => new Partition()
                {
                    PartitionOffset = x.Offset,
                    DeviceId = x.diskId.ToString(),
                    PartitionCapacity = x.Size,
                    ExtentType = Enum.GetName(typeof(VDS_DISK_EXTENT_TYPE), x.type)
                }).ToList();
            }

            foreach (var volume in vdsVolumes)
            {
                List<VDS_DISK_EXTENT> vdsExtents = new List<VDS_DISK_EXTENT>();
                VDS_FILE_SYSTEM_PROP vdsFileSystemProp = new VDS_FILE_SYSTEM_PROP();

                hr = volume.GetProperties(out VDS_VOLUME_PROP volumeProps);
                if (hr != 0 && hr != HR_PROPERTIES_INCOMPLETE)
                {
                    LogEvent($"Unable to get volume data from VDS. Exception: {Marshal.GetExceptionForHR(hr).Message}", Region.Hardware, EventType.ERROR);
                    continue;
                }

                var matchingPartitions = drives.SelectMany(x => x.Partitions).Where(x => x.DeviceId == volumeProps.Id.ToString());

                foreach (var plex in volume.GetVolumePlexes())
                {
                    hr = plex.QueryExtents(out VDS_DISK_EXTENT[] extents, out _);
                    if (hr != 0)
                    {
                        LogEvent($"Unable to get volume plexes from VDS for volume {volumeProps.Name}. Exception: {Marshal.GetExceptionForHR(hr).Message}", Region.Hardware, EventType.ERROR);
                        continue;
                    }

                    vdsExtents.AddRange(extents);
                }


                if (volume is IVdsVolumeMF volumeMf)
                {
                    hr = volumeMf.GetFileSystemProperties(out VDS_FILE_SYSTEM_PROP fileSystemProp);
                    if (hr != 0 && hr != HR_PROPERTIES_INCOMPLETE)
                    {
                        LogEvent($"Unable to get file system data from VDS. Exception: {Marshal.GetExceptionForHR(hr).Message}", Region.Hardware, EventType.ERROR);
                    }

                    vdsFileSystemProp = fileSystemProp;
                }

                foreach (var partition in drives.SelectMany(x => x.Partitions).Where(x => vdsExtents.Any(y => y.Offset == x.PartitionOffset && y.diskId.ToString() == x.DeviceId)))
                {
                    partition.VolumeId = volumeProps.Id.ToString();
                    partition.DirtyBitSet = (volumeProps.Flags ^ VDS_VOLUME_FLAG.DIRTY) == VDS_VOLUME_FLAG.DIRTY;
                    partition.Filesystem = Enum.GetName(typeof(VDS_FILE_SYSTEM_TYPE), vdsFileSystemProp.Type);
                    partition.PartitionLabel = string.Copy(vdsFileSystemProp.Label ?? string.Empty);
                    partition.PartitionCapacity = volumeProps.Size;
                    partition.PartitionFree = vdsFileSystemProp.AllocationUnitSize * vdsFileSystemProp.AvailableAllocationUnits;
                    partition.VolumeType = Enum.GetName(typeof(VDS_VOLUME_TYPE), volumeProps.Type);
                }

            }

            foreach (var driveLetter in driveLetters.Where(x => x.bUsed))
            {
                foreach (var partition in drives.SelectMany(x => x.Partitions).Where(x => x.VolumeId == driveLetter.volumeId.ToString()))
                {
                    partition.PartitionLetter = driveLetter.wcLetter.ToString();
                }
            }

            foreach (var drive in drives)
            {
                drive.DiskFree = (ulong)drive.Partitions.Sum(x => (long)x.PartitionFree);
            }

            return drives;

        }

        [ComImport, Guid("9c38ed61-d565-4728-aeee-c80952f0ecde")]
        public class VdsServiceLoader
        {

        }

        private static IEnumerable<IVdsDisk> GetDisks(IEnumerable<IVdsPack> vdsPacks)
        {
            foreach (var pack in vdsPacks)
            {
                Marshal.ThrowExceptionForHR(pack.QueryDisks(out IEnumVdsObject diskEnum));
                while (0 == diskEnum.Next(1, out object iface, out _))
                {
                    yield return iface as IVdsDisk;
                }
            }
        }

        private static IEnumerable<IVdsVolume> GetVolumes(IEnumerable<IVdsPack> vdsPacks)
        {
            foreach (var pack in vdsPacks)
            {
                pack.QueryVolumes(out IEnumVdsObject volumeEnum);
                while (0 == volumeEnum.Next(1, out object iface, out _))
                {
                    yield return iface as IVdsVolume;
                }
            }
        }

        private static IEnumerable<IVdsVolumePlex> GetVolumePlexes(this IVdsVolume pack)
        {
            Marshal.ThrowExceptionForHR(pack.QueryPlexes(out IEnumVdsObject plexEnum));
            while (0 == plexEnum.Next(1, out object iface, out _))
            {
                yield return iface as IVdsVolumePlex;
            }
        }

        private static IEnumerable<IVdsProvider> GetProviders(IVdsService vdsService, VDS_QUERY_PROVIDER_FLAG providerMask = VDS_QUERY_PROVIDER_FLAG.SOFTWARE_PROVIDERS)
        {
            Marshal.ThrowExceptionForHR(vdsService.QueryProviders(providerMask, out IEnumVdsObject vEnum));
            while (0 == vEnum.Next(1, out object iface, out _))
            {
                yield return iface as IVdsProvider;
            }
        }

        private static IEnumerable<IVdsPack> GetPacks(IVdsService vdsService)
        {
            foreach (IVdsSwProvider swProvider in GetProviders(vdsService, VDS_QUERY_PROVIDER_FLAG.SOFTWARE_PROVIDERS).OfType<IVdsSwProvider>())
            {
                if (swProvider == null)
                    continue;

                Marshal.ThrowExceptionForHR(swProvider.QueryPacks(out IEnumVdsObject vEnum));
                while (0 == vEnum.Next(1, out object iface, out _))
                {
                    yield return iface as IVdsPack;
                }
            }
        }

    }
}
