using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using System.Linq;
using NTFSLib.Utilities;

namespace NTFSLib.IO
{
    public abstract class NtfsFileEntry
    {
        protected NTFS Ntfs;
        public FileRecord MFTRecord { get; private set; }

        internal AttributeFileName FileName;
        private AttributeStandardInformation _standardInformation;

        public DateTime TimeCreation
        {
            get { return _standardInformation == null ? DateTime.MinValue : _standardInformation.TimeCreated; }
        }
        public DateTime TimeModified
        {
            get { return _standardInformation == null ? DateTime.MinValue : _standardInformation.TimeModified; }
        }
        public DateTime TimeAccessed
        {
            get { return _standardInformation == null ? DateTime.MinValue : _standardInformation.TimeAccessed; }
        }
        public DateTime TimeMftModified
        {
            get { return _standardInformation == null ? DateTime.MinValue : _standardInformation.TimeMftModified; }
        }

        public string Name
        {
            get { return FileName.FileName; }
        }

        public NtfsDirectory Parent
        {
            get
            {
                return CreateEntry(FileName.ParentDirectory.FileId) as NtfsDirectory;
            }
        }

        protected NtfsFileEntry(NTFS ntfs, FileRecord record, AttributeFileName fileName)
        {
            Ntfs = ntfs;
            MFTRecord = record;

            FileName = fileName;

            Init();
        }

        private void Init()
        {
            _standardInformation = MFTRecord.Attributes.OfType<AttributeStandardInformation>().SingleOrDefault();
        }

        internal NtfsFileEntry CreateEntry(uint fileId, AttributeFileName fileName = null)
        {
            return CreateEntry(Ntfs, fileId, fileName);
        }

        internal static NtfsFileEntry CreateEntry(NTFS ntfs, uint fileId, AttributeFileName fileName = null)
        {
            if (fileName == null)
            {
                // Dig up a preferred name
                FileRecord tmpRecord = ntfs.ReadMFTRecord(fileId);
                fileName = NtfsUtils.GetPreferredDisplayName(tmpRecord);
            }

            NtfsFileEntry entry = ntfs.FileCache.Get(fileId, fileName.FileName.GetHashCode());

            if (entry != null)
            {
                Debug.WriteLine("Got from cache: " + fileId + ":" + fileName.Id);
                return entry;
            }

            // Create it
            FileRecord record = ntfs.ReadMFTRecord(fileId);

            if (record.Flags.HasFlag(FileEntryFlags.Directory))
                entry = new NtfsDirectory(ntfs, record, fileName);
            else
                entry = new NtfsFile(ntfs, record, fileName);

            ntfs.FileCache.Set(fileId, fileName.Id, entry);

            return entry;
        }

        public string[] GetStreamList()
        {
            return MFTRecord.Attributes.OfType<AttributeData>().Select(s => s.AttributeName).ToArray();
        }

        public Stream OpenRead(string dataStream = "")
        {
            if (Ntfs.Provider.MftFileOnly)
                throw new InvalidOperationException("Provider indicates it's providing an MFT file only");

            // Get all DATA attributes
            List<AttributeData> dataAttribs = MFTRecord.Attributes.OfType<AttributeData>().Where(s => s.AttributeName == dataStream).ToList();

            Debug.Assert(dataAttribs.Count >= 1);
            if (dataAttribs.Count > 1)
                Debugger.Break();

            if (dataAttribs.Count == 1 && dataAttribs[0].NonResidentFlag == ResidentFlag.Resident)
            {
                return new MemoryStream(dataAttribs[0].DataBytes);
            }

            Debug.Assert(dataAttribs.All(s => s.NonResidentFlag == ResidentFlag.NonResident));

            DataFragment[] fragments = dataAttribs.SelectMany(s => s.DataFragments).OrderBy(s => s.StartingVCN).ToArray();
            Stream diskStream = Ntfs.Provider.CreateDiskStream();

            ushort compressionUnitSize = dataAttribs[0].NonResidentHeader.CompressionUnitSize;
            ushort compressionClusterCount = (ushort)(compressionUnitSize == 0 ? 0 : Math.Pow(2, compressionUnitSize));

            return new NtfsDiskStream(Ntfs, diskStream, fragments, compressionClusterCount, (long)dataAttribs[0].NonResidentHeader.ContentSize);
        }
    }
}