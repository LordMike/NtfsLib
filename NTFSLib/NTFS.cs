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
        private WeakReference[] FileRecords { get; set; }

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
            if (Provider.IsFile)
            {
                Boot = new BootSector();
                Boot.OEMCode = "NTFS";
                Boot.SectorsPrCluster = 2;      // Small cluster
                Boot.BytesPrSector = 512;       // Smallest possible sector
            }
            else
            {
                byte[] data = Provider.ReadBytes(0, 512);
                Boot = BootSector.ParseData(data, 512, 0);

                Debug.Assert(Boot.OEMCode == "NTFS");
            }

            // Get FileRecord size
            RefreshFileRecordSize();

            // Read $MFT file record
            {
                byte[] data = ReadMFTRecordData(0);
                FileMft = ParseMFTRecord(data);
            }

            Debug.Assert(FileMft.Attributes.Count(s => s.Type == AttributeType.DATA) == 1);
            AttributeData fileMftData = FileMft.Attributes.OfType<AttributeData>().Single();
            Debug.Assert(fileMftData.NonResidentFlag == ResidentFlag.NonResident);
            Debug.Assert(fileMftData.DataFragments.Length >= 1);

            // Get number of FileRecords 
            FileRecordCount = (uint)(fileMftData.DataFragments.Sum(s => (decimal)(s.ClusterCount * BytesPrCluster)) / BytesPrFileRecord);
            FileRecords = new WeakReference[FileRecordCount];

            // Read $VOLUME file record
            FileRecord fileVolume = ReadMFTRecord(SpecialMFTFiles.Volume);

            // Get version
            Attribute versionAttrib = fileVolume.Attributes.SingleOrDefault(s => s.Type == AttributeType.VOLUME_INFORMATION);
            if (versionAttrib != null)
            {
                AttributeVolumeInformation attrib = (AttributeVolumeInformation)versionAttrib;

                NTFSVersion = new Version(attrib.MajorVersion, attrib.MinorVersion);
            }

            // Read primary records
            for (uint i = 0; i < 24; i++)
            {
                ReadMFTRecord(i);
            }

        }

        private void RefreshFileRecordSize()
        {
            byte[] data;
            if (Provider.IsFile)
            {
                // Get the first 512 bytes of the provider
                data = Provider.ReadBytes(0, 512);
            }
            else
            {
                // Not continous, adhere to $BOOT
                data = Provider.ReadBytes(Boot.MFTCluster * BytesPrCluster, 512);
            }

            BytesPrFileRecord = FileRecord.ParseAllocatedSize(data, 0);

            Debug.WriteLine("Updated BytesPrFileRecord, now set to " + BytesPrFileRecord);
        }

        private FileRecord ReadMFTRecord(SpecialMFTFiles file)
        {
            return ReadMFTRecord((uint)file);
        }

        private FileRecord ParseMFTRecord(byte[] data)
        {
            FileRecord record = FileRecord.ParseHeader(data, 0);
            record.ApplyUSNPatch(data);
            record.ParseAttributes(data, (uint)data.Length, record.OffsetToFirstAttribute);

            return record;
        }

        private FileRecord ReadMFTRecord(uint number)
        {
            if (number <= FileRecords.Length && FileRecords[number] != null && FileRecords[number].IsAlive)
            {
                return (FileRecord)FileRecords[number].Target;
            }

            byte[] data = ReadMFTRecordData(number);
            FileRecord record = ParseMFTRecord(data);

            FileRecords[number] = new WeakReference(record);

            // Check size
            if (BytesPrFileRecord == 0)
            {
                // Some checks
                Debug.Assert(record.SizeOfFileRecordAllocated % 512 == 0);
                Debug.Assert(record.SizeOfFileRecordAllocated >= 512);
                Debug.Assert(record.SizeOfFileRecordAllocated <= 4096);

                BytesPrFileRecord = record.SizeOfFileRecordAllocated;
            }

            return record;
        }

        private byte[] ReadMFTRecordData(uint number)
        {
            ulong offset;
            int length = (int)(BytesPrFileRecord == 0 ? 4096 : BytesPrFileRecord);

            // Calculate location
            if (Provider.IsFile)
            {
                // Is a continous file - ignore MFT fragments
                offset = (ulong)(number * length);
            }
            else if (FileMft == null)
            {
                // We haven't got the $MFT yet, ignore MFT fragments
                offset = (ulong)(number * length + (decimal)(Boot.MFTCluster * BytesPrCluster));
            }
            else
            {
                // Find fragment(s)
                AttributeData dataAttribute = FileMft.Attributes.OfType<AttributeData>().First();

                Debug.Assert(dataAttribute.NonResidentFlag == ResidentFlag.NonResident);

                uint fileOffset = (uint)(number * length);
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
