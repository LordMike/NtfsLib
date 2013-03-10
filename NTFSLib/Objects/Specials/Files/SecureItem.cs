using System.IO;
using NTFSLib.Utilities;

namespace NTFSLib.Objects.Specials.Files
{
    public class SecureItem
    {
        public uint Hash { get; set; }
        public uint Sid { get; set; }
        public ulong OffsetToThisEntry { get; set; }
        public uint Size { get; set; }

        public static SecureItem ParseSingle(Stream stream)
        {
            SecureItem item = new SecureItem();

            item.Hash = stream.ReadUint();
            item.Sid = stream.ReadUint();
            item.OffsetToThisEntry = stream.ReadUlong();
            item.Size = stream.ReadUint();

            stream.SkipBytes((int) (item.Size - (stream.Position - (long)item.OffsetToThisEntry)));

            // Pad to 16
            stream.SkipBytes( (int) (16 - (stream.Position % 16)));

            //0x00	4	Hash of Security Descriptor
            //0x04	4	Security Id
            //0x08	8	Offset of this entry in this file
            //0x10	4	Size of this entry
            //0x04	V	Self-relative Security Descriptor
            //V+0x04	P16	Padding

            return item;
        }
    }
}