using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NTFSLib.IO;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Specials;
using NTFSLib.Provider;
using NTFSLib.Utilities;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NTFSLib
{
    public class NTFS
    {
        internal IDiskProvider Provider { get; private set; }
        private WeakReference[] FileRecords { get; set; }
        internal NtfsFileCache FileCache { get; private set; }
        private Stream MftStream { get; set; }
        private RawDiskCache MftRawCache { get; set; }

        public NTFS(IDiskProvider provider)
        {
            Provider = provider;
            FileCache = new NtfsFileCache();
            MftRawCache = new RawDiskCache(128 * 1024 * 1024);     // 128 MB

            InitializeNTFS();
        }

        public uint BytesPrCluster
        {
            get { return (uint)(Boot.BytesPrSector * Boot.SectorsPrCluster); }
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
            if (Provider.MftFileOnly)
            {
                Boot = new BootSector();
                Boot.OEMCode = "NTFS";
                Boot.SectorsPrCluster = 2;      // Small cluster
                Boot.BytesPrSector = 512;       // Smallest possible sector
            }
            else
            {
                byte[] data = new byte[512];
                Provider.ReadBytes(data, 0, 0, 512);
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

            MftStream = OpenFileRecord(FileMFT);

            // Get number of FileRecords 
            FileRecordCount = (uint)(fileMftData.DataFragments.Sum(s => (float)s.Clusters) * (BytesPrCluster / BytesPrFileRecord));
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

        public void PrepRawDiskCache(uint number)
        {
            Debug.Assert(MftStream != null);
            Debug.Assert(BytesPrFileRecord > 0);

            uint offset = number * BytesPrFileRecord;
            int toRead = (int)Math.Min(MftStream.Length - offset, MftRawCache.Data.Length);

            // Read
            MftStream.Position = offset;
            MftStream.Read(MftRawCache.Data, 0, toRead);

            // Set props
            MftRawCache.DataOffset = offset;
            MftRawCache.Length = toRead;
        }

        public bool InRawDiskCache(uint number)
        {
            if (MftRawCache.Initialized && MftRawCache.DataOffset / BytesPrFileRecord <= number &&
                number <= MftRawCache.DataOffset / BytesPrFileRecord + MftRawCache.Length / BytesPrFileRecord - 1)
                return true;

            return false;
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
            byte[] data = new byte[512];
            if (Provider.MftFileOnly)
            {
                // Get the first 512 bytes of the provider
                Provider.ReadBytes(data, 0, 0, 512);
            }
            else
            {
                // Not continous, adhere to $BOOT
                Provider.ReadBytes(data, 0, Boot.MFTCluster * BytesPrCluster, 512);
            }

            BytesPrFileRecord = FileRecord.ParseAllocatedSize(data, 0);

            Debug.WriteLine("Updated BytesPrFileRecord, now set to " + BytesPrFileRecord);
        }

        public void ParseNonResidentAttributes(FileRecord record)
        {
            if (Provider.MftFileOnly)
                // Nothing to do about this
                throw new InvalidOperationException("Provider indicates an MFT file is used. Cannot parse non-resident attributes.");

            foreach (Attribute attr in record.Attributes.Where(s => s.Type != AttributeType.DATA && s.NonResidentFlag == ResidentFlag.NonResident))
            {
                ParseNonResidentAttribute(attr);
            }
        }

        public void ParseNonResidentAttribute(Attribute attr)
        {
            if (Provider.MftFileOnly)
                // Nothing to do about this
                throw new InvalidOperationException("Provider indicates an MFT file is used. Cannot parse non-resident attributes.");

            if (attr.NonResidentHeader.Fragments.Length > 0)
                // Get data
                attr.ParseAttributeNonResidentBody(this);
        }

        public void ParseAttributeLists(FileRecord record)
        {
            while (record.Attributes.Any(s => s.Type == AttributeType.ATTRIBUTE_LIST))
            {
                AttributeList listAttr = record.Attributes.OfType<AttributeList>().First();

                if (listAttr.NonResidentFlag == ResidentFlag.NonResident)
                {
                    if (Provider.MftFileOnly)
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
            NtfsUtils.ApplyUSNPatch(data, 0, data.Length / Boot.BytesPrSector, Boot.BytesPrSector, record.USNNumber, record.USNData);
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

        public string BuildFileName(FileRecord record)
        {
            return BuildFileName(record, null);
        }

        public string BuildFileName(FileRecord record, char rootDriveLetter)
        {
            return BuildFileName(record, rootDriveLetter + ":");
        }

        public string BuildFileName(FileRecord record, string rootName)
        {
            // Get filename (and prefer the non-8dot3 variant)
            AttributeFileName fileName = NtfsUtils.GetPreferredDisplayName(record);

            if (fileName == null)
                throw new NullReferenceException("Record has no FileName attribute");

            string path = fileName.FileName;

            if (record.Flags.HasFlag(FileEntryFlags.Directory))
                path += '\\';

            // Continue till we hit SpecialMFTFiles.RootDir
            FileRecord parentRecord;
            do
            {
                // Get parent
                parentRecord = ReadMFTRecord((uint)fileName.ParentDirectory.FileId);

                if (parentRecord == null)
                    throw new NullReferenceException("A parent record was null");

                fileName = NtfsUtils.GetPreferredDisplayName(parentRecord);

                if (fileName == null)
                    throw new NullReferenceException("A parent record had no Filename attribute");

                if (parentRecord.FileReference.FileId == (uint)SpecialMFTFiles.RootDir)
                {
                    path = rootName + '\\' + path;
                    break;
                }
                path = fileName.FileName + '\\' + path;
            } while (true);

            return path;
        }

        public byte[] ReadMFTRecordData(uint number)
        {
            long offset;
            int length = (int)(BytesPrFileRecord == 0 ? 4096 : BytesPrFileRecord);

            // Calculate location
            if (InRawDiskCache(number))
            {
                byte[] mftData = new byte[length];
                int cacheOffset = (int)(number * length - MftRawCache.DataOffset);

                Array.Copy(MftRawCache.Data, cacheOffset, mftData, 0, mftData.Length);

                return mftData;
            }

            if (Provider.MftFileOnly)
            {
                // Is a continous file - ignore MFT fragments
                offset = number * length;
            }
            else if (FileMFT == null)
            {
                // We haven't got the $MFT yet, ignore MFT fragments
                offset = number * length + (long)(Boot.MFTCluster * BytesPrCluster);
            }
            else if (MftStream != null)
            {
                uint fileOffset = (uint)(number * length);
                byte[] mftData = new byte[length];

                MftStream.Position = fileOffset;
                MftStream.Read(mftData, 0, length);

                Debug.WriteLine("Read MFT Record {0} via. mft ntfsdiskstream; bytes {1}->{2} ({3} bytes)", number, fileOffset, fileOffset + (decimal)length, length);
                return mftData;
            }
            else
            {
                throw new Exception("Shouldn't happen");
            }

            if (!Provider.CanReadBytes((ulong)offset, length))
            {
                Debug.WriteLine("Couldn't read MFT Record {0}; bytes {1}->{2} ({3} bytes)", number, offset, offset + (decimal)length, length);
                return new byte[0];
            }

            Debug.WriteLine("Read MFT Record {0}; bytes {1}->{2} ({3} bytes)", number, offset, offset + (decimal)length, length);
            byte[] data = new byte[length];
            Provider.ReadBytes(data, 0, (ulong)offset, length);

            return data;
        }

        public Stream OpenFileRecord(uint number, string dataStream = "")
        {
            return OpenFileRecord(ReadMFTRecord(number), dataStream);
        }

        public Stream OpenFileRecord(FileRecord record, string dataStream = "")
        {
            Debug.Assert(record != null);

            if (Provider.MftFileOnly)
                throw new InvalidOperationException("Provider indicates it's providing an MFT file only");

            // Fetch extended data
            ParseAttributeLists(record);

            // Get all DATA attributes
            List<AttributeData> dataAttribs = record.Attributes.OfType<AttributeData>().Where(s => s.AttributeName == dataStream).ToList();

            Debug.Assert(dataAttribs.Count >= 1);

            if (dataAttribs.Count == 1 && dataAttribs[0].NonResidentFlag == ResidentFlag.Resident)
            {
                return new MemoryStream(dataAttribs[0].DataBytes);
            }

            Debug.Assert(dataAttribs.All(s => s.NonResidentFlag == ResidentFlag.NonResident));

            DataFragment[] fragments = dataAttribs.SelectMany(s => s.DataFragments).OrderBy(s => s.StartingVCN).ToArray();
            Stream diskStream = Provider.CreateDiskStream();

            ushort compressionUnitSize = dataAttribs[0].NonResidentHeader.CompressionUnitSize;
            ushort compressionClusterCount = (ushort)Math.Pow(2, compressionUnitSize);

            return new NtfsDiskStream(this, diskStream, fragments, compressionClusterCount, (long)dataAttribs[0].NonResidentHeader.ContentSize);
        }

        public NtfsDirectory GetRootDirectory()
        {
            return (NtfsDirectory)NtfsFileEntry.CreateEntry(this, (uint)SpecialMFTFiles.RootDir);
        }
    }
}
