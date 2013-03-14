using System;
using System.Diagnostics;

namespace NTFSLib.Objects
{
    public class FileReference : IEquatable<FileReference>
    {
        public ulong RawId { get; set; }
        public uint FileId { get; set; }
        public ushort FileSequenceNumber { get; set; }

        public FileReference(ulong rawId)
        {
            FileSequenceNumber = (ushort)(rawId >> 48);     // Get the high-order 16 bites
            FileId = (uint) (rawId & 0xFFFFFFFFUL);         // Get the low-order 32 bits

            ushort middleSpace = (ushort) ((rawId >> 32) & 0xFFFFUL);    // Get the 16 bits in-between the Id and the SequenceNumber
            Debug.Assert(middleSpace == 0);

            RawId = rawId;
        }

        public FileReference(uint fileId, ushort sequenceNumber)
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

        public override int GetHashCode()
        {
            return RawId.GetHashCode();
        }
    }
}