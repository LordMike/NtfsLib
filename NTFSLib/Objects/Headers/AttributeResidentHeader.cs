using System;
using System.Diagnostics;
using NTFSLib.Utilities;

namespace NTFSLib.Objects.Headers
{
    public class AttributeResidentHeader : ISaveableObject
    {
        public uint ContentLength { get; set; }
        public ushort ContentOffset { get; set; }

        public static AttributeResidentHeader ParseHeader(byte[] data, int offset = 0)
        {
            Debug.Assert(data.Length - offset >= 6);
            Debug.Assert(offset >= 0);

            AttributeResidentHeader res = new AttributeResidentHeader();

            res.ContentLength = BitConverter.ToUInt32(data, offset);
            res.ContentOffset = BitConverter.ToUInt16(data, offset + 4);

            return res;
        }

        public int GetSaveLength()
        {
            return 6;
        }

        public void Save(byte[] buffer, int offset)
        {
            Debug.Assert(buffer.Length - offset >= GetSaveLength());

            LittleEndianConverter.GetBytes(buffer, offset, ContentLength);
            LittleEndianConverter.GetBytes(buffer, offset + 4, ContentOffset);
        }

    }
}