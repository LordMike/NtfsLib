using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Specials;
using NTFSLib.Provider;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NTFSLib
{
    public class NTFS
    {
        internal IDiskProvider Provider { get; private set; }
        private WeakReference[] FileRecords { get; set; }

        public NTFS(IDiskProvider provider)
        {
            Provider = provider;

            InitializeNTFS();
        }

        public ulong BytesPrCluster
        {
            get { return (ulong)(Boot.BytesPrSector * Boot.SectorsPrCluster); }
        }

        public uint BytesPrFileRecord { get; private set; }
        public uint FileRecordCount { get; private set; }

        public BootSector Boot { get; private set; }
        public FileRecord FileMFT { get; private set; }
        public FileRecord FileMFTMirr { get; private set; }
        public FileRecord FileLogFile { get; private set; }
        public FileRecord FileVolume { get; private set; }
        public FileRecord FileAttrDef { get; private set; }
        public FileRecord FileRootDir { get; private set; }
        public FileRecord FileBitmap { get; private set; }
        public FileRecord FileBoot { get; private set; }
        public FileRecord FileBadClus { get; private set; }
        //public FileRecord FileQuota { get; private set; }
        public FileRecord FileSecure { get; private set; }
        public FileRecord FileUpCase { get; private set; }
        public FileRecord FileExtend { get; private set; }

        public Version NTFSVersion { get; private set; }

        private void InitializeNTFS()
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
                byte[] data = ReadMFTRecordData((uint)SpecialMFTFiles.MFT);
                FileMFT = ParseMFTRecord(data);
            }

            Debug.Assert(FileMFT.Attributes.Count(s => s.Type == AttributeType.DATA) == 1);
            AttributeData fileMftData = FileMFT.Attributes.OfType<AttributeData>().Single();
            Debug.Assert(fileMftData.NonResidentFlag == ResidentFlag.NonResident);
            Debug.Assert(fileMftData.DataFragments.Length >= 1);

            // Get number of FileRecords 
            FileRecordCount = (uint)(fileMftData.DataFragments.Sum(s => (decimal)(s.ClusterCount * BytesPrCluster)) / BytesPrFileRecord);
            FileRecords = new WeakReference[FileRecordCount];

            FileRecords[0] = new WeakReference(FileMFT);

            // Read $VOLUME file record
            FileRecord fileVolume = ReadMFTRecord(SpecialMFTFiles.Volume);

            // Get version
            Attribute versionAttrib = fileVolume.Attributes.SingleOrDefault(s => s.Type == AttributeType.VOLUME_INFORMATION);
            if (versionAttrib != null)
            {
                AttributeVolumeInformation attrib = (AttributeVolumeInformation)versionAttrib;

                NTFSVersion = new Version(attrib.MajorVersion, attrib.MinorVersion);
            }
        }

        public void InitializeCommon()
        {
            // Read primary records
            FileMFTMirr = ReadMFTRecord(SpecialMFTFiles.MFTMirr);
            FileLogFile = ReadMFTRecord(SpecialMFTFiles.LogFile);
            FileVolume = ReadMFTRecord(SpecialMFTFiles.Volume);
            FileAttrDef = ReadMFTRecord(SpecialMFTFiles.AttrDef);
            FileRootDir = ReadMFTRecord(SpecialMFTFiles.RootDir);
            FileBitmap = ReadMFTRecord(SpecialMFTFiles.Bitmap);
            FileBoot = ReadMFTRecord(SpecialMFTFiles.Boot);
            FileBadClus = ReadMFTRecord(SpecialMFTFiles.BadClus);
            //FileQuota = ReadMFTRecord(SpecialMFTFiles.Quota);
            FileSecure = ReadMFTRecord(SpecialMFTFiles.Secure);
            FileUpCase = ReadMFTRecord(SpecialMFTFiles.UpCase);
            FileExtend = ReadMFTRecord(SpecialMFTFiles.Extend);

            // Read extended data
            foreach (SpecialMFTFiles specialMFTFile in Enum.GetValues(typeof(SpecialMFTFiles)).OfType<SpecialMFTFiles>())
            {
                WeakReference item = FileRecords[(int)specialMFTFile];
                if (item != null && item.IsAlive)
                {
                    ParseAttributeLists((FileRecord)item.Target);
                }
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

        public void ParseAttributeLists(FileRecord record)
        {
            while (record.Attributes.Any(s => s.Type == AttributeType.ATTRIBUTE_LIST))
            {
                AttributeList listAttr = record.Attributes.OfType<AttributeList>().First();

                if (listAttr.NonResidentFlag == ResidentFlag.NonResident)
                {
                    if (Provider.IsFile)
                    {
                        // Nothing to do about this
                        return;
                    }

                    // Get data
                    listAttr.ParseAttributeNonResidentBody(this);
                }

                foreach (AttributeListItem item in listAttr.Items)
                {
                    if (item.BaseFile.Equals(record.FileReference))
                        // Skip own attributes
                        continue;

                    FileRecord otherRecord = ReadMFTRecord((uint)item.BaseFile.FileId);

                    Debug.Assert(otherRecord.FileReference.Equals(item.BaseFile));

                    List<Attribute> otherAttrib = otherRecord.Attributes.Where(s => s.Id == item.AttributeId).ToList();

                    Debug.Assert(otherAttrib.Count == 1);
                    Debug.Assert(otherAttrib[0].Type == item.Type);

                    record.Attributes.AddRange(otherAttrib);
                }

                record.Attributes.Remove(listAttr);
            }
        }

        private FileRecord ParseMFTRecord(byte[] data)
        {
            FileRecord record = FileRecord.ParseHeader(data, 0);
            record.ApplyUSNPatch(data);
            record.ParseAttributes(data, (uint)data.Length, record.OffsetToFirstAttribute);

            return record;
        }

        public FileRecord ReadMFTRecord(SpecialMFTFiles file)
        {
            return ReadMFTRecord((uint)file);
        }

        public FileRecord ReadMFTRecord(uint number)
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

        public byte[] ReadMFTRecordData(uint number)
        {
            ulong offset;
            int length = (int)(BytesPrFileRecord == 0 ? 4096 : BytesPrFileRecord);

            // Calculate location
            if (Provider.IsFile)
            {
                // Is a continous file - ignore MFT fragments
                offset = (ulong)(number * length);
            }
            else if (FileMFT == null)
            {
                // We haven't got the $MFT yet, ignore MFT fragments
                offset = (ulong)(number * length + (decimal)(Boot.MFTCluster * BytesPrCluster));
            }
            else
            {
                // Find fragment(s)
                AttributeData dataAttribute = FileMFT.Attributes.OfType<AttributeData>().First();

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

        public Stream OpenFileRecord(uint number, string dataStream = "")
        {
            return OpenFileRecord(ReadMFTRecord(number), dataStream);
        }

        public Stream OpenFileRecord(FileRecord record, string dataStream = "")
        {
            Debug.Assert(record != null);

            // Fetch extended data
            ParseAttributeLists(record);

            // Get all DATA attributes
            List<AttributeData> dataAttribs = record.Attributes.OfType<AttributeData>().Where(s => (s.NonResidentFlag == ResidentFlag.Resident && s.ResidentHeader.AttributeName == dataStream) || (s.NonResidentFlag == ResidentFlag.NonResident && s.NonResidentHeader.AttributeName == dataStream)).ToList();

            Debug.Assert(dataAttribs.Count == 1);
            AttributeData dataAttrib = dataAttribs.First();

            if (dataAttrib.NonResidentFlag == ResidentFlag.Resident)
            {
                return new MemoryStream(dataAttrib.DataBytes);
            }

            Debug.Assert(dataAttrib.NonResidentFlag == ResidentFlag.NonResident);

            DataFragment[] fragments = dataAttrib.DataFragments.OrderBy(s => s.StartingVCN).ToArray();

            return new NtfsDiskStream(this, fragments, (long)dataAttrib.NonResidentHeader.ContentSize);
        }
    }
}
