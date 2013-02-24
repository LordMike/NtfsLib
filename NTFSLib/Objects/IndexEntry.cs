using System;
using System.Diagnostics;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects
{
    public class IndexEntry
    {
        public FileReference FileRefence { get; set; }
        public ushort Size { get; set; }
        public ushort StreamSize { get; set; }
        public MFTIndexEntryFlags Flags { get; set; }
        public byte[] Stream { get; set; }

        public static IndexEntry ParseData(byte[] data, int maxLength, int offset)
        {
            Debug.Assert(maxLength >= 16);

            IndexEntry res = new IndexEntry();

            res.FileRefence = new FileReference(BitConverter.ToUInt64(data, offset));
            res.Size = BitConverter.ToUInt16(data, offset + 8);
            res.StreamSize = BitConverter.ToUInt16(data, offset + 10);
            res.Flags = (MFTIndexEntryFlags)data[offset + 12];

            Debug.Assert(maxLength >= res.Size);
            Debug.Assert(maxLength >= 16 + res.StreamSize);

            res.Stream = new byte[res.StreamSize];
            Array.Copy(data, offset + 16, res.Stream, 0, res.StreamSize);

            return res;
        }
    }
}