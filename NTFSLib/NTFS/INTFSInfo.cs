using System.IO;

namespace NTFSLib.NTFS
{
    public interface INTFSInfo
    {
        uint BytesPrCluster { get; }
        uint BytesPrSector { get; }
        byte SectorsPrCluster { get; }

        bool OwnsDiskStream { get; }
        Stream GetDiskStream();
    }
}
