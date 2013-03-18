using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NTFSLib.Objects.Enums;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NTFSLib.Objects
{
    public class FileRecord
    {
        public string Signature { get; set; }
        public ushort OffsetToUSN { get; set; }
        public ushort USNSizeWords { get; set; }
        public ulong LogFileUSN { get; set; }
        public ushort SequenceNumber { get; set; }
        public short HardlinkCount { get; set; }
        public ushort OffsetToFirstAttribute { get; set; }
        public FileEntryFlags Flags { get; set; }
        public uint SizeOfFileRecord { get; set; }
        public uint SizeOfFileRecordAllocated { get; set; }
        public FileReference BaseFile { get; set; }
        public ushort NextFreeAttributeId { get; set; }
        public uint MFTNumber { get; set; }
        public byte[] USNNumber { get; set; }
        public byte[] USNData { get; set; }

        public List<Attribute> Attributes { get; set; }

        public static uint ParseAllocatedSize(byte[] data, int offset)
        {
            Debug.Assert(data.Length - offset >= 4);

            return BitConverter.ToUInt32(data, offset + 28);
        }

        public FileReference FileReference { get; set; }

        public bool IsExtensionRecord
        {
            get { return BaseFile.RawId != 0; }
        }

        public static FileRecord ParseHeader(byte[] data, int offset)
        {
            Debug.Assert(data.Length - offset >= 50);

            FileRecord res = new FileRecord();

            res.Signature = Encoding.ASCII.GetString(data, offset + 0, 4);
            Debug.Assert(res.Signature == "FILE");

            res.OffsetToUSN = BitConverter.ToUInt16(data, offset + 4);
            res.USNSizeWords = BitConverter.ToUInt16(data, offset + 6);
            res.LogFileUSN = BitConverter.ToUInt64(data, offset + 8);
            res.SequenceNumber = BitConverter.ToUInt16(data, offset + 16);
            res.HardlinkCount = BitConverter.ToInt16(data, offset + 18);
            res.OffsetToFirstAttribute = BitConverter.ToUInt16(data, offset + 20);
            res.Flags = (FileEntryFlags)BitConverter.ToUInt16(data, offset + 22);
            res.SizeOfFileRecord = BitConverter.ToUInt32(data, offset + 24);
            res.SizeOfFileRecordAllocated = BitConverter.ToUInt32(data, offset + 28);
            res.BaseFile = new FileReference(BitConverter.ToUInt64(data, offset + 32));
            res.NextFreeAttributeId = BitConverter.ToUInt16(data, offset + 40);
            // Two unused bytes here
            res.MFTNumber = BitConverter.ToUInt32(data, offset + 44);

            res.USNNumber = new byte[2];
            Array.Copy(data, offset + res.OffsetToUSN, res.USNNumber, 0, 2);

            Debug.Assert(data.Length - offset >= res.OffsetToUSN + 2 + res.USNSizeWords * 2);

            res.USNData = new byte[res.USNSizeWords * 2 - 2];
            Array.Copy(data, offset + res.OffsetToUSN + 2, res.USNData, 0, res.USNData.Length);

            res.FileReference = new FileReference(res.MFTNumber, res.SequenceNumber);

            return res;
        }

        public void ParseAttributes(byte[] data, uint maxLength, int offset)
        {
            Debug.Assert(Signature == "FILE");

            Attributes = new List<Attribute>();
            int attribOffset = offset;
            for (int attribId = 0; ; attribId++)
            {
                AttributeType attributeType = Attribute.GetType(data, attribOffset);
                if (attributeType == AttributeType.EndOfAttributes)
                    break;

                uint length = Attribute.GetTotalLength(data, attribOffset);

                Debug.Assert(attribOffset + length <= maxLength);

                Attribute attrib = Attribute.ParseSingleAttribute(data, (int) length, attribOffset);
                Attributes.Add(attrib);

                attribOffset += attrib.TotalLength;
            }
        }
    }
}