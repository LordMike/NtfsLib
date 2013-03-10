using System.IO;
using System.Text;
using NTFSLib.Utilities;

namespace NTFSLib.Objects.Specials.Files
{
    public class AttrDefItem
    {
        public string Label { get; set; }
        public uint Type { get; set; }
        public uint DisplayRule { get; set; }
        public AttrDefCollationRule CollationRule { get; set; }
        public AttrDefType Flags { get; set; }
        public ulong MinimumSize { get; set; }
        public ulong MaximumSize { get; set; }

        public static AttrDefItem ParseSingle(Stream stream)
        {
            AttrDefItem item = new AttrDefItem();

            item.Label = stream.ReadString(Encoding.Unicode, 128);
            item.Type = stream.ReadUint();
            item.DisplayRule = stream.ReadUint();
            item.CollationRule = (AttrDefCollationRule)stream.ReadInt();
            item.Flags = (AttrDefType)stream.ReadInt();
            item.MinimumSize = stream.ReadUlong();
            item.MaximumSize = stream.ReadUlong();

            return item;
        }
    }
}