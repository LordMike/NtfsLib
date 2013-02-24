using System;
using System.Text;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NTFSLib.Objects.Headers
{
    public class AttributeNonResidentHeader
    {
        public ulong StartingVCN { get; set; }
        public ulong EndingVCN { get; set; }
        public ushort ListOffset { get; set; }
        public ushort Compression { get; set; }
        public ulong ContentSizeAllocated { get; set; }
        public ulong ContentSize { get; set; }
        public ulong ContentSizeInitialized { get; set; }
        //public ulong ContentSizeCompressed { get; set; }
        public string AttributeName { get; set; }

        public static AttributeNonResidentHeader ParseHeader(Attribute parent, byte[] data, int offset = 0)
        {
            AttributeNonResidentHeader res = new AttributeNonResidentHeader();

            res.StartingVCN = BitConverter.ToUInt64(data, offset);
            res.EndingVCN = BitConverter.ToUInt64(data, offset + 8);
            res.ListOffset = BitConverter.ToUInt16(data, offset + 16);
            res.Compression = BitConverter.ToUInt16(data, offset + 18);
            res.ContentSizeAllocated = BitConverter.ToUInt64(data, offset + 24);
            res.ContentSize = BitConverter.ToUInt64(data, offset + 32);
            res.ContentSizeInitialized = BitConverter.ToUInt64(data, offset + 40);
            //res.ContentSizeCompressed = BitConverter.ToUInt64(data, offset + 48);
            if (parent.NameLength == 0)
                res.AttributeName = string.Empty;
            else
                res.AttributeName = Encoding.Unicode.GetString(data, offset + parent.OffsetToName - 16, parent.NameLength * 2);

            return res;
        }

        public DataFragment[] NonResidentFragments { get; set; }
    }
}