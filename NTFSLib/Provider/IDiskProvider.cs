namespace NTFSLib.Provider
{
    public interface IDiskProvider
    {
        /// <summary>
        /// Is the backing datasource a file?
        /// True : F.ex. if it's an extracted $MFT 
        /// False: F.ex. if it's a disk drive (or an image thereof)
        /// </summary>
        bool IsFile { get; }

        bool CanReadBytes(ulong offset, int bytes);
        byte[] ReadBytes(ulong offset, int bytes);
    }
}