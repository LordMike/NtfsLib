namespace NTFSLib.Objects
{
    //public class DiskProvider : IMFTProvider
    //{
    //    //public RawDisk Disk { get; set; }

    //    //public DiskProvider(RawDisk disk)
    //    //{
    //    //    Disk = disk;
    //    //}

    //    //// Read in clusters
    //    //public byte[] Read(ulong cluster, int clusters)
    //    //{
    //    //    return Disk.Read((long)cluster, clusters);
    //    //}

    //    // Read in bytes
    //    //public byte[] Read(ulong offsetBytes, int bytes)
    //    //{
    //    //    // Determine cluster
    //    //    ulong cluster = offsetBytes / (ulong)Disk.ClusterSize;
    //    //    int clusters = bytes / Disk.ClusterSize + (bytes % Disk.ClusterSize != 0 ? 1 : 0);

    //    //    byte[] data = Disk.Read((long)cluster, clusters);

    //    //    if (data.Length == bytes && offsetBytes % (ulong)Disk.ClusterSize == 0)
    //    //        return data;

    //    //    // Get chunk
    //    //    byte[] tmpData = new byte[bytes];
    //    //    Array.Copy(data, (int)(offsetBytes % (ulong)Disk.ClusterSize), tmpData, 0, bytes);

    //    //    return tmpData;
    //    //}
    //}
}