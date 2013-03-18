using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NTFSLib.NTFS;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Utilities;

namespace NTFSLib.IO
{
    public abstract class NtfsFileEntry
    {
        protected NTFSWrapper NTFSWrapper;
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

        protected NtfsFileEntry(NTFSWrapper ntfsWrapper, FileRecord record, AttributeFileName fileName)
        {
            NTFSWrapper = ntfsWrapper;
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
            return CreateEntry(NTFSWrapper, fileId, fileName);
        }

        internal static NtfsFileEntry CreateEntry(NTFSWrapper ntfsWrapper, uint fileId, AttributeFileName fileName = null)
        {
            if (fileName == null)
            {
                // Dig up a preferred name
                FileRecord tmpRecord = ntfsWrapper.ReadMFTRecord(fileId);
                fileName = NtfsUtils.GetPreferredDisplayName(tmpRecord);
            }

            NtfsFileEntry entry = ntfsWrapper.FileCache.Get(fileId, fileName.FileName.GetHashCode());

            if (entry != null)
            {
                Debug.WriteLine("Got from cache: " + fileId + ":" + fileName.Id);
                return entry;
            }

            // Create it
            FileRecord record = ntfsWrapper.ReadMFTRecord(fileId);

            if (record.Flags.HasFlag(FileEntryFlags.Directory))
                entry = new NtfsDirectory(ntfsWrapper, record, fileName);
            else
                entry = new NtfsFile(ntfsWrapper, record, fileName);

            ntfsWrapper.FileCache.Set(fileId, fileName.Id, entry);

            return entry;
        }

        public string[] GetStreamList()
        {
            return MFTRecord.Attributes.OfType<AttributeData>().Select(s => s.AttributeName).ToArray();
        }

        public Stream OpenRead(string dataStream = "")
        {
            if (NTFSWrapper.Provider.MftFileOnly)
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
            Stream diskStream = NTFSWrapper.Provider.CreateDiskStream();

            ushort compressionUnitSize = dataAttribs[0].NonResidentHeader.CompressionUnitSize;
            ushort compressionClusterCount = (ushort)(compressionUnitSize == 0 ? 0 : Math.Pow(2, compressionUnitSize));

            return new NtfsDiskStream(diskStream, true, fragments, NTFSWrapper.BytesPrCluster, compressionClusterCount, (long)dataAttribs[0].NonResidentHeader.ContentSize);
        }
    }
}