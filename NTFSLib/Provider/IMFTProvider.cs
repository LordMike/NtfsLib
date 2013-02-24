namespace NTFSLib.Provider
{
    internal interface IMFTProvider
    {
        byte[] Read(ulong cluster, int clusters);
    }
}