using System;
using System.Diagnostics;
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

        public uint BytesPrFileRecord { get; private set; }
        public uint FileRecordCount { get; private set; }

        public BootSector Boot { get; private set; }
        public FileRecord FileMft { get; private set; }

        public Version NTFSVersion { get; private set; }

        private void Initialize()
        {
            // Read $BOOT
            byte[] data;
            if (Provider.IsFile)
            {
                Boot = new BootSector();
                Boot.OEMCode = "NTFS";
                Boot.SectorsPrCluster = 2;      // Small cluster
                Boot.BytesPrSector = 512;       // Smallest possible sector
            }
            else
            {
                data = Provider.ReadBytes(0, 512);
                Boot = BootSector.ParseData(data, 512, 0);

                Debug.Assert(Boot.OEMCode == "NTFS");
            }

            // Get FileRecord size
            data = Provider.ReadBytes(Boot.MFTCluster * BytesPrCluster, 512);

            BytesPrFileRecord = FileRecord.ParseAllocatedSize(data, 0);

            // Read $MFT file record
            data = Provider.ReadBytes(Boot.MFTCluster * BytesPrCluster, (int)BytesPrFileRecord);

            FileMft = FileRecord.ParseHeader(data, 0);
            FileMft.ApplyUSNPatch(data);
            FileMft.ParseAttributes(data, (uint)data.Length, FileMft.OffsetToFirstAttribute);

            Debug.Assert(FileMft.Attributes.Count(s => s.Type == AttributeType.DATA) == 1);
            AttributeData fileMftData = FileMft.Attributes.OfType<AttributeData>().Single();
            Debug.Assert(fileMftData.DataFragments.Length >= 1);

            // Get number of FileRecords 
            FileRecordCount = (uint)(fileMftData.DataFragments.Sum(s => (decimal)(s.ClusterCount * BytesPrCluster)) / BytesPrFileRecord);

            // Read $VOLUME file record
            data = ReadMFTRecordData(3);

            FileRecord fileVolume = FileRecord.ParseHeader(data, 0);
            fileVolume.ApplyUSNPatch(data);
            fileVolume.ParseAttributes(data, (uint)data.Length, FileMft.OffsetToFirstAttribute);

            // Get version
            Attribute versionAttrib = fileVolume.Attributes.SingleOrDefault(s => s.Type == AttributeType.VOLUME_INFORMATION);
            if (versionAttrib != null)
            {
                AttributeVolumeInformation attrib = (AttributeVolumeInformation)versionAttrib;

                NTFSVersion = new Version(attrib.MajorVersion, attrib.MinorVersion);
            }

            FileRecord[] res = new FileRecord[512000];

            // Read primary records
            for (int i = 0; i < FileRecordCount; i++)
            {
                data = ReadMFTRecordData((uint)i);

                FileRecord test = FileRecord.ParseHeader(data, 0);
                test.ApplyUSNPatch(data);
                test.ParseAttributes(data, (uint)data.Length, test.OffsetToFirstAttribute);

                Console.WriteLine(test.MFTNumber);
                //Console.WriteLine(string.Join(", ", test.Attributes.Select(s => s.Type.ToString())));

                res[i] = test;
            }

            Console.WriteLine(FileMft.Attributes.OfType<AttributeData>().First().DataFragments.Length);
        }

        private byte[] ReadMFTRecordData(uint number)
        {
            ulong offset;
            int length = (int)BytesPrFileRecord;

            // Calculate location
            if (Provider.IsFile)
            {
                // Is a continous file - ignore MFT fragments
                offset = number * BytesPrFileRecord + Boot.MFTCluster * BytesPrCluster;
            }
            else
            {
                // Find fragment(s)
                AttributeData dataAttribute = FileMft.Attributes.OfType<AttributeData>().First();

                Debug.Assert(dataAttribute.NonResidentFlag == ResidentFlag.NonResident);

                uint fileOffset = number * BytesPrFileRecord;
                ulong fileVcn = fileOffset / BytesPrCluster;
                decimal lengthClusters = length / (decimal)BytesPrCluster;

                // Find relevant fragment
                DataFragment fragment = null;

                for (int i = 0; i < dataAttribute.DataFragments.Length; i++)
                {
                    DataFragment tmpFragment = dataAttribute.DataFragments[i];
                    if (tmpFragment.StartingVCN <= fileVcn &&
                        tmpFragment.StartingVCN + tmpFragment.ClusterCount >= fileVcn + lengthClusters)
                    {
                        fragment = tmpFragment;
                        break;
                    }
                }

                Debug.Assert(fragment != null);

                // Calculate offset inside fragment
                ulong fragmentOffset = fragment.StartingVCN * BytesPrCluster;
                ulong fileOffsetInFragment = fileOffset - fragmentOffset;

                offset = fragment.LCN * BytesPrCluster + fileOffsetInFragment;
            }

            if (!Provider.CanReadBytes(offset, length))
            {
                Debug.WriteLine("Couldn't read MFT Record {0}; bytes {1}->{2} ({3} bytes)", number, offset, offset + (decimal)length, length);
                return new byte[0];
            }

            Debug.WriteLine("Read MFT Record {0}; bytes {1}->{2} ({3} bytes)", number, offset, offset + (decimal)length, length);
            return Provider.ReadBytes(offset, length);
        }
    }
}
