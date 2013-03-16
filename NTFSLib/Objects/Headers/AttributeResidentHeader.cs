using System;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NTFSLib.Objects.Headers
{
    public class AttributeResidentHeader
    {
        public uint ContentLength { get; set; }
        public ushort ContentOffset { get; set; }

        public static AttributeResidentHeader ParseHeader(Attribute parent, byte[] data, int offset = 0)
        {
            AttributeResidentHeader res = new AttributeResidentHeader();

            res.ContentLength = BitConverter.ToUInt32(data, offset);
            res.ContentOffset = BitConverter.ToUInt16(data, offset + 4);

            return res;
        }
    }
}