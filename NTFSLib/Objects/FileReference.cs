using System;

namespace NTFSLib.Objects
{
    public class FileReference : IEquatable<FileReference>
    {
        public ulong RawId { get; set; }
        public ulong FileId { get; set; }
        public ushort FileSequenceNumber { get; set; }

        public FileReference(ulong rawId)
        {
            FileSequenceNumber = (ushort)(rawId >> 48);
            FileId = rawId & 0x00000000FFFFFFFFUL;
            RawId = rawId;
        }

        public FileReference(ulong fileId, ushort sequenceNumber)
        {
            FileSequenceNumber = sequenceNumber;
            FileId = fileId;
            RawId = ((ulong)sequenceNumber << 48) | fileId;
        }

        public override string ToString()
        {
            return FileId + ":" + FileSequenceNumber;
        }

        public bool Equals(FileReference other)
        {
            if (other == null)
                return false;

            if (other == this)
                return true;

            return other.RawId == RawId;
        }

        public override bool Equals(object obj)
        {
            FileReference other = obj as FileReference;

            return Equals(other);
        }
    }
}