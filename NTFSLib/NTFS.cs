using System;
using System.Diagnostics;
using System.Text;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Specials;
using NTFSLib.Provider;
using System.Linq;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

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

        public ulong BytesPrCluster
        {
            get { return (ulong)(Boot.BytesPrSector * Boot.SectorsPrCluster); }
        }

        public BootSector Boot { get; private set; }
        public FileRecord FileMft { get; private set; }

        public Version NTFSVersion { get; private set; }

        private void Initialize()
        {
            // Read $BOOT
            byte[] data = Provider.ReadBytes(0, 512);

            Boot = BootSector.ParseData(data, 512, 0);

            Debug.Assert(Boot.OEMCode == "NTFS");

            // Read $MFT file record
            data = Provider.ReadBytes(Boot.MFTCluster * BytesPrCluster, (int)(Boot.MFTRecordSizeClusters * BytesPrCluster));

            FileMft = FileRecord.ParseHeader(data, 0);
            FileMft.ParseAttributes(data, (uint)data.Length, FileMft.OffsetToFirstAttribute);

            for (int i = 0; i < 25; i++)
            {
                // Read $BOOT file record
                data = Provider.ReadBytes((ulong)(i * 2 * (decimal)Boot.BytesPrSector + Boot.MFTCluster * BytesPrCluster), (int)(Boot.MFTRecordSizeClusters * BytesPrCluster));

                FileRecord fileBoot = FileRecord.ParseHeader(data, 0);
                fileBoot.ParseAttributes(data, (uint)data.Length, FileMft.OffsetToFirstAttribute);

                // Get version
                Attribute versionAttrib = fileBoot.Attributes.SingleOrDefault(s => s.Type == AttributeType.VOLUME_INFORMATION);
                if (versionAttrib != null)
                {
                    AttributeVolumeInformation attrib = (AttributeVolumeInformation)versionAttrib;

                    NTFSVersion = new Version(attrib.MajorVersion, attrib.MinorVersion);
                }
            }
        }

        private byte[] ReadMFTRecordData(uint number)
        {
            var offset = number * 2 * Boot.BytesPrSector + Boot.MFTCluster * BytesPrCluster;
            var length = Boot.MFTRecordSizeClusters * BytesPrCluster;
        }
    }
}
