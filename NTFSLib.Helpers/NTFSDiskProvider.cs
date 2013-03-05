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

        public int ReadBytes(byte[] buffer, int bufferOffset, ulong offset, int bytes)
        {
            long sector = (long)offset / _disk.SectorSize;
            int sectors = bytes / _disk.SectorSize + (bytes % _disk.SectorSize == 0 ? 0 : 1);

            // Read sectors
            // TODO: Make it so that _disk.ReadSectors() can take a byte array
            byte[] data = _disk.ReadSectors(sector, sectors);

            Array.Copy(data, bytes % _disk.SectorSize, buffer, bufferOffset, bytes);

            return bytes;
        }
    }
}
