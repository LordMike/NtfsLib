using System.Collections.Generic;
using System.IO;

namespace NTFSLib.Objects.Specials.Files
{
    public class Secure
    {
        public SecureItem[] Items { get; set; }

        public static Secure ParseFile(Stream stream)
        {
            List<SecureItem> items = new List<SecureItem>();

            // Parse
            while (stream.Length > stream.Position)
            {
                SecureItem item = SecureItem.ParseSingle(stream);

                items.Add(item);
            } 

            // Return
            Secure res = new Secure();
            res.Items = items.ToArray();

            return res;
        }
    }
}