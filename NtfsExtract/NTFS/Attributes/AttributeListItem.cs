using System;
using System.Diagnostics;
using System.Text;
using NtfsExtract.NTFS.Enums;
using NtfsExtract.NTFS.Objects;

namespace NtfsExtract.NTFS.Attributes
{
    public class AttributeListItem
    {
        public AttributeType Type { get; set; }
        public ushort Length { get; set; }
        public byte NameLength { get; set; }
        public byte NameOffset { get; set; }
        public ulong StartingVCN { get; set; }
        public FileReference BaseFile { get; set; }
        public ushort AttributeId { get; set; }
        public string Name { get; set; }

        public static AttributeListItem ParseListItem(byte[] data, int maxLength, int offset)
        {
            Debug.Assert(maxLength >= 26);

            AttributeListItem res = new AttributeListItem();

            res.Type = (AttributeType)BitConverter.ToUInt32(data, offset);
            res.Length = BitConverter.ToUInt16(data, offset + 4);
            res.NameLength = data[offset + 6];
            res.NameOffset = data[offset + 7];
            res.StartingVCN = BitConverter.ToUInt64(data, offset + 8);
            res.BaseFile = new FileReference(BitConverter.ToUInt64(data, offset + 16));
            res.AttributeId = BitConverter.ToUInt16(data, offset + 24);

            Debug.Assert(maxLength >= res.NameOffset + res.NameLength * 2);
            res.Name = Encoding.Unicode.GetString(data, offset + res.NameOffset, res.NameLength * 2);

            return res;
        }
    }
}