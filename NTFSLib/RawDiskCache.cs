namespace NTFSLib
{
    public class RawDiskCache
    {
        public uint DataOffset { get; set; }
        public int Length { get; set; }
        public byte[] Data { get; set; }

        public bool Initialized
        {
            get { return Length != 0; }
        }

        public RawDiskCache(int size)
        {
            DataOffset = 0;
            Length = 0;
            Data = new byte[size];
        }
    }
}