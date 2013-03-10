using System.Collections.Generic;
using System.IO;

namespace NTFSLib.Objects.Specials.Files
{
    public class AttrDef
    {
        public AttrDefItem[] Items { get; set; }

        public static AttrDef ParseFile(Stream stream)
        {
            List<AttrDefItem> items = new List<AttrDefItem>();

            // Parse
            do
            {
                AttrDefItem item = AttrDefItem.ParseSingle(stream);

                if (item.Type == 0)
                    break;

                items.Add(item);
            } while (true);

            // Return
            AttrDef res = new AttrDef();
            res.Items = items.ToArray();

            return res;
        }
    }
}
