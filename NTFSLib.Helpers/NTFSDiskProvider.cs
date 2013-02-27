using System;
using NTFSLib.Provider;
using RawDiskLib;

namespace NTFSLib.Helpers
{
    public class NTFSDiskProvider : IDiskProvider
    {
        private RawDisk _disk;

        public NTFSDiskProvider(RawDisk disk)
        {
            _disk = disk;
        }

        public bool IsFile
        {
            get { return false; }
        }

        public bool CanReadBytes(ulong offset, int bytes)
        {
            if (bytes <= 0)
                return false;

            if (offset + (ulong)bytes > (ulong)_disk.SizeBytes)
                return false;

            return true;
        }

        public byte[] ReadBytes(ulong offset, int bytes)
        {
            long sector = (long)offset / _disk.SectorSize;
            int sectors = bytes / _disk.SectorSize + (bytes % _disk.SectorSize == 0 ? 1 : 0);

            // Read sectors
            byte[] tmpData = _disk.ReadSectors(sector, sectors);

            if (tmpData.Length == bytes)
                return tmpData;

            byte[] data = new byte[bytes];
            Array.Copy(tmpData, bytes % _disk.SectorSize, data, 0, bytes);

            return data;
        }
    }
}
