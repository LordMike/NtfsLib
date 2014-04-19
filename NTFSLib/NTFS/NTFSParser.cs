using System;
using System.Collections;
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
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NTFSLib.NTFS
{
    public class NTFSParser : INTFSInfo
    {
        private Stream _diskStream;
        private Stream _mftStream;
        private BootSector _boot;
        private FileRecord _mftRecord;
        private BitArray _usedRecords;

        private uint _sectorsPrRecord;
        public uint BytesPrFileRecord { get; private set; }
        public uint BytesPrCluster { get { return (uint)(_boot.BytesPrSector * _boot.SectorsPrCluster); } }
        public uint BytesPrSector { get { return _boot.BytesPrSector; } }
        public byte SectorsPrCluster { get { return _boot.SectorsPrCluster; } }
        public ulong TotalSectors { get { return _boot.TotalSectors; } }
        public ulong TotalClusters { get { return _boot.TotalSectors / _boot.SectorsPrCluster; } }

        public bool OwnsDiskStream
        {
            get { return false; }
        }
        public Stream GetDiskStream()
        {
            return _diskStream;
        }

        public uint CurrentMftRecordNumber { get; set; }
        public uint FileRecordCount { get; private set; }

        public NTFSParser(Stream diskStream)
        {
            _diskStream = diskStream;

            InitiateBoot();
            InitiateMFT();
        }

        private void InitiateBoot()
        {
            // Read first 512 bytes
            byte[] data = new byte[512];
            _diskStream.Seek(0, SeekOrigin.Begin);
            _diskStream.Read(data, 0, data.Length);

            // Parse boot
            _boot = BootSector.ParseData(data, data.Length, 0);

            // Get filerecord size
            BytesPrFileRecord = _boot.MFTRecordSizeBytes;
            _sectorsPrRecord = _boot.MFTRecordSizeBytes / _boot.BytesPrSector;
        }

        private void InitiateMFT()
        {
            // Read first FileRecord
            _buffer = new byte[BytesPrFileRecord];
            _diskStream.Seek((long)(_boot.MFTCluster * BytesPrCluster), SeekOrigin.Begin);
            _diskStream.Read(_buffer, 0, _buffer.Length);

            // Parse
            FileRecord record = FileRecord.Parse(_buffer, 0, _boot.BytesPrSector, _sectorsPrRecord);

            // TODO: Parse nonresident $DATA's
            _mftRecord = record;

            // Prep an NTFSDiskStream
            List<AttributeData> dataAttribs = _mftRecord.Attributes.OfType<AttributeData>().Where(s => s.AttributeName == string.Empty && s.NonResidentFlag == ResidentFlag.NonResident).ToList();
            DataFragment[] fragments = dataAttribs.SelectMany(s => s.DataFragments).OrderBy(s => s.StartingVCN).ToArray();

            ushort compressionUnitSize = dataAttribs[0].NonResidentHeader.CompressionUnitSize;
            ushort compressionClusterCount = (ushort)(compressionUnitSize == 0 ? 0 : Math.Pow(2, compressionUnitSize));

            _mftStream = new NtfsDiskStream(_diskStream, false, fragments, BytesPrCluster, compressionClusterCount, (long)dataAttribs[0].NonResidentHeader.ContentSize);

            CurrentMftRecordNumber = 0;
            FileRecordCount = (uint)(_mftStream.Length / BytesPrFileRecord);
        }

        public void InitiateRecordBitarray()
        {
            if (_usedRecords != null)
                return;

            // Read $MFT Bitmap
            AttributeBitmap bitmapAttrib = _mftRecord.Attributes.OfType<AttributeBitmap>().Single();

            ParseNonResidentAttribute(bitmapAttrib);
            _usedRecords = bitmapAttrib.Bitfield;
        }

        public void ParseNonResidentAttribute(Attribute attr)
        {
            if (attr.NonResidentFlag == ResidentFlag.NonResident && attr.NonResidentHeader.Fragments.Length > 0)
                // Get data
                attr.ParseAttributeNonResidentBody(this);
        }

        private byte[] _buffer;

        public FileRecord ParseNextRecord()
        {
            Debug.Assert(_buffer.Length == BytesPrFileRecord);
            Debug.Assert(0 != BytesPrFileRecord);

            uint newPosition = CurrentMftRecordNumber * BytesPrFileRecord;
            if (_mftStream.Position != newPosition)
                _mftStream.Seek(newPosition, SeekOrigin.Begin);

            int read = _mftStream.Read(_buffer, 0, _buffer.Length);

            if (read == 0)
                return null;

            // Parse
            FileRecord record = FileRecord.Parse(_buffer, 0, _boot.BytesPrSector, _sectorsPrRecord);

            // Increment number
            CurrentMftRecordNumber = record.MFTNumber + 1;

            return record;
        }

        public IEnumerable<FileRecord> GetRecords(bool skipUnused = false)
        {
            if (skipUnused && _usedRecords == null)
            {
                // Initiate _usedRecords
                InitiateRecordBitarray();
            }

            while (true)
            {
                if (skipUnused && !_usedRecords[(int)CurrentMftRecordNumber])
                {
                    // Skip to the next used record
                    for (int i = (int)CurrentMftRecordNumber + 1; i < FileRecordCount; i++)
                    {
                        CurrentMftRecordNumber = (uint)i;

                        if (_usedRecords[i])
                            // Use this
                            break;
                    }
                }

                FileRecord record = ParseNextRecord();

                if (record == null)
                    break;

                yield return record;
            }
        }

        public Stream OpenFileDataStream(Stream diskStream, FileRecord record, string dataStream = "")
        {
            Debug.Assert(record != null);

            // Get all DATA attributes
            List<AttributeData> dataAttribs = record.Attributes.OfType<AttributeData>().Where(s => s.AttributeName == dataStream).ToList();

            if (!dataAttribs.Any())
            {
                return new MemoryStream();
            }

            if (dataAttribs.Count == 1 && dataAttribs[0].NonResidentFlag == ResidentFlag.Resident)
            {
                return new MemoryStream(dataAttribs[0].DataBytes);
            }

            Debug.Assert(dataAttribs.All(s => s.NonResidentFlag == ResidentFlag.NonResident));

            DataFragment[] fragments = dataAttribs.SelectMany(s => s.DataFragments).OrderBy(s => s.StartingVCN).ToArray();

            ushort compressionUnitSize = dataAttribs[0].NonResidentHeader.CompressionUnitSize;
            ushort compressionClusterCount = (ushort)(compressionUnitSize == 0 ? 0 : Math.Pow(2, compressionUnitSize));

            return new NtfsDiskStream(diskStream, true, fragments, BytesPrCluster, compressionClusterCount, (long)dataAttribs[0].NonResidentHeader.ContentSize);
        }
    }
}
