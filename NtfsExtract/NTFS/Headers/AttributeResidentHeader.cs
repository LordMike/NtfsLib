using System;
using System.Diagnostics;

namespace NtfsExtract.NTFS.Headers
{
    public class AttributeResidentHeader 
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
    }
}