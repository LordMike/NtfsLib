namespace NTFSLib.Provider
{
    public interface IDiskProvider
    {
        bool CanReadBytes(ulong offset, int bytes);
        byte[] ReadBytes(ulong offset, int bytes);
    }
}