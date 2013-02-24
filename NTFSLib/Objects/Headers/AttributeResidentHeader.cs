using System;
using System.Text;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NTFSLib.Objects.Headers
{
    public class AttributeResidentHeader
    {
        public uint ContentLength { get; set; }
        public ushort ContentOffset { get; set; }
        public string AttributeName { get; set; }

        public static AttributeResidentHeader ParseHeader(Attribute parent, byte[] data, int offset = 0)
        {
            AttributeResidentHeader res = new AttributeResidentHeader();

            res.ContentLength = BitConverter.ToUInt32(data, offset);
            res.ContentOffset = BitConverter.ToUInt16(data, offset + 4);
            if (parent.NameLength == 0)
                res.AttributeName = string.Empty;
            else
                res.AttributeName = Encoding.Unicode.GetString(data, offset + parent.OffsetToName - 16, parent.NameLength * 2);

            return res;
        }
    }
}