using System;
using System.Diagnostics;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Headers;

namespace NTFSLib.Objects
{
    public class IndexEntry
    {
        public FileReference FileRefence { get; set; }
        public ushort Size { get; set; }
        public ushort StreamSize { get; set; }
        public MFTIndexEntryFlags Flags { get; set; }
        public byte[] Stream { get; set; }

        public AttributeFileName ChildFileName { get; set; }

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


            if (res.StreamSize > 66)
            {
                res.ChildFileName = new AttributeFileName();
                res.ChildFileName.ParseAttributeResidentBody(res.Stream, res.StreamSize, 0);

                // Fake the resident header
                res.ChildFileName.ResidentHeader = new AttributeResidentHeader();
                res.ChildFileName.ResidentHeader.AttributeName = string.Empty;
                res.ChildFileName.ResidentHeader.ContentLength = res.StreamSize;
                res.ChildFileName.ResidentHeader.ContentOffset = 0;
            }

            return res;
        }

        public override string ToString()
        {
            if (ChildFileName == null)
                return FileRefence.ToString();
            return FileRefence + " (" + ChildFileName.FileName + ")";
        }
    }
}