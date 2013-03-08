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

        public bool MftFileOnly
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

        public int ReadBytes(byte[] buffer, int bufferOffset, ulong offset, int bytes)
        {
            long sector = (long)offset / _disk.SectorSize;
            int sectors = bytes / _disk.SectorSize + (bytes % _disk.SectorSize == 0 ? 0 : 1);

            // Read sectors
            return _disk.ReadSectors(buffer, bufferOffset, sector, sectors);
        }
    }
}
