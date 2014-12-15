using System;
using System.Diagnostics;

namespace NtfsExtract.NTFS.Objects
{
    public class FileReference : IEquatable<FileReference>, IComparable<FileReference>
    {
        public ulong RawId { get; set; }
        public uint FileId { get; set; }
        public ushort FileSequenceNumber { get; set; }

        public FileReference(ulong rawId)
        {
            FileSequenceNumber = (ushort)(rawId >> 48);     // Get the high-order 16 bites
            FileId = (uint)(rawId & 0xFFFFFFFFUL);         // Get the low-order 32 bits

            ushort middleSpace = (ushort)((rawId >> 32) & 0xFFFFUL);    // Get the 16 bits in-between the Id and the SequenceNumber
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
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileReference);
        }

        public override int GetHashCode()
        {
            return RawId.GetHashCode();
        }

        public static bool operator ==(FileReference a, FileReference b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if ((object)a == null || (object)b == null)
                return false;
            return a.RawId == b.RawId;
        }

        public static bool operator !=(FileReference a, FileReference b)
        {
            return !(a == b);
        }

        public static bool operator <(FileReference a, FileReference b)
        {
            if (ReferenceEquals(a, b))
                return false;
            if ((object)a == null)
                throw new ArgumentNullException("a");
            if ((object)b == null)
                throw new ArgumentNullException("b");

            return CompareToInternal(a, b) < 0;
        }

        public static bool operator >(FileReference a, FileReference b)
        {
            if (ReferenceEquals(a, b))
                return false;
            if ((object)a == null)
                throw new ArgumentNullException("a");
            if ((object)b == null)
                throw new ArgumentNullException("b");

            return CompareToInternal(a, b) > 0;
        }

        public static bool operator <=(FileReference a, FileReference b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if ((object)a == null)
                throw new ArgumentNullException("a");
            if ((object)b == null)
                throw new ArgumentNullException("b");

            return CompareToInternal(a, b) <= 0;
        }

        public static bool operator >=(FileReference a, FileReference b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if ((object)a == null)
                throw new ArgumentNullException("a");
            if ((object)b == null)
                throw new ArgumentNullException("b");

            return CompareToInternal(a, b) >= 0;
        }

        public int CompareTo(FileReference other)
        {
            // <0   This < other
            // 0    This == other
            // >0   This > other

            if ((object)other == null)
                return 1;

            return CompareToInternal(this, other);
        }

        private static int CompareToInternal(FileReference a, FileReference b)
        {
            // <0   a <  b
            // 0    a == b
            // >0   a >  b

            if (a.FileId == b.FileId)
            {
                // Compare sequence numbers
                if (a.FileSequenceNumber < b.FileSequenceNumber)
                    return -1;
                if (a.FileSequenceNumber > b.FileSequenceNumber)
                    return 1;

                // Both Id and Seq num are identical
                return 0;
            }

            if (a.FileId < b.FileId)
                return -1;

            // FileId > other.FileId
            return 1;
        }
    }
}