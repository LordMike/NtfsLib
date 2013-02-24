namespace NTFSLib.Objects
{
    public class FileReference
    {
        public ulong RawId { get; set; }
        public ulong FileId
        {
            get { return RawId & 0x00000000FFFFFFFFUL; }
        }
        public ushort FileSequenceNumber
        {
            get { return (ushort)(RawId >> 48); }
        }

        public FileReference(ulong rawId)
        {
            RawId = rawId;
        }

        public override string ToString()
        {
            return FileId + ":" + FileSequenceNumber;
        }
    }
}