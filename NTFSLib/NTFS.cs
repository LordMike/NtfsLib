using System.Diagnostics;
using System.Text;
using NTFSLib.Objects.Specials;
using NTFSLib.Provider;

namespace NTFSLib
{
    public class NTFS
    {
        private IDiskProvider Provider { get; set; }

        public NTFS(IDiskProvider provider)
        {
            Provider = provider;

            Initialize();
        }

        private BootSector _boot;

        private void Initialize()
        {
            // Read $BOOT
            byte[] boot = Provider.ReadBytes(0, 512);

            _boot = BootSector.ParseData(boot, 512, 0);

            Debug.Assert(Encoding.ASCII.GetString(boot, 3, 4) == "NTFS");
        }
    }
}
