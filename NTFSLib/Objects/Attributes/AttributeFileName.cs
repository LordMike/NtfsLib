using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NTFSLib.Objects.Enums;
using NTFSLib.Utilities;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeFileName : Attribute
    {
        public FileReference ParentDirectory { get; set; }
        public DateTime CTime { get; set; }
        public DateTime ATime { get; set; }
        public DateTime MTime { get; set; }
        public DateTime RTime { get; set; }
        public ulong AllocatedSize { get; set; }
        public ulong RealSize { get; set; }
        public FileAttributes FileFlags { get; set; }
        public uint ReservedEAsReparse { get; set; }
        public byte FilenameLength { get; set; }
        public FileNamespace FilenameNamespace { get; set; }
        public string FileName { get; set; }

        public override AttributeResidentAllow AllowedResidentStates
        {
            get
            {
                return AttributeResidentAllow.Resident;
            }
        }

        internal override void ParseAttributeResidentBody(byte[] data, int maxLength, int offset)
        {
            base.ParseAttributeResidentBody(data, maxLength, offset);

            ParentDirectory = new FileReference(BitConverter.ToUInt64(data, offset));
            CTime = NtfsUtils.FromWinFileTime(data, offset + 8);
            ATime = NtfsUtils.FromWinFileTime(data, offset + 16);
            MTime = NtfsUtils.FromWinFileTime(data, offset + 24);
            RTime = NtfsUtils.FromWinFileTime(data, offset + 32);
            AllocatedSize = BitConverter.ToUInt64(data, offset + 40);
            RealSize = BitConverter.ToUInt64(data, offset + 48);
            FileFlags = (FileAttributes)BitConverter.ToInt32(data, offset + 56);
            ReservedEAsReparse = BitConverter.ToUInt32(data, offset + 60);
            FilenameLength = data[offset + 64];
            FilenameNamespace = (FileNamespace) data[offset + 65];

            Debug.Assert(maxLength >= 66 + FilenameLength * 2);

            FileName = Encoding.Unicode.GetString(data, offset + 66, FilenameLength * 2);
        }
    }
}