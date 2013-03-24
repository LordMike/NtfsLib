using System;
using System.Diagnostics;
using System.IO;
using NTFSLib.Objects.Enums;
using NTFSLib.Utilities;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeStandardInformation : Attribute
    {
        public DateTime TimeCreated { get; set; }
        public DateTime TimeModified { get; set; }
        public DateTime TimeMftModified { get; set; }
        public DateTime TimeAccessed { get; set; }
        public FileAttributes DosPermissions { get; set; }
        public uint MaxmiumVersions { get; set; }
        public uint VersionNumber { get; set; }
        public uint ClassId { get; set; }
        public uint OwnerId { get; set; }
        public uint SecurityId { get; set; }
        public ulong QuotaCharged { get; set; }
        public ulong USN { get; set; }

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

            Debug.Assert(maxLength >= 48);

            TimeCreated = NtfsUtils.FromWinFileTime(data, offset);
            TimeModified = NtfsUtils.FromWinFileTime(data, offset + 8);
            TimeMftModified = NtfsUtils.FromWinFileTime(data, offset + 16);
            TimeAccessed = NtfsUtils.FromWinFileTime(data, offset + 24);
            DosPermissions = (FileAttributes)BitConverter.ToInt32(data, offset + 32);

            MaxmiumVersions = BitConverter.ToUInt32(data, offset + 36);
            VersionNumber = BitConverter.ToUInt32(data, offset + 40);
            ClassId = BitConverter.ToUInt32(data, offset + 44);

            // The below fields are for version 3.0+
            if (NonResidentFlag == ResidentFlag.Resident && ResidentHeader.ContentLength >= 72)
            {
                Debug.Assert(maxLength >= 72);

                OwnerId = BitConverter.ToUInt32(data, offset + 48);
                SecurityId = BitConverter.ToUInt32(data, offset + 52);
                QuotaCharged = BitConverter.ToUInt64(data, offset + 56);
                USN = BitConverter.ToUInt64(data, offset + 64);
            }
        }

        public override int GetSaveLength()
        {
            // TODO: Get the actual NTFS Version in here to check against
            if (OwnerId != 0 || SecurityId != 0 || QuotaCharged != 0 || USN != 0)
            {
                return base.GetSaveLength() + 72;
            }

            return base.GetSaveLength() + 48;
        }

        public override void Save(byte[] buffer, int offset)
        {
            base.Save(buffer, offset);

            LittleEndianConverter.GetBytes(buffer, offset, TimeCreated);
            LittleEndianConverter.GetBytes(buffer, offset + 8, TimeModified);
            LittleEndianConverter.GetBytes(buffer, offset + 16, TimeMftModified);
            LittleEndianConverter.GetBytes(buffer, offset + 24, TimeAccessed);
            LittleEndianConverter.GetBytes(buffer, offset + 32, (int)DosPermissions);

            LittleEndianConverter.GetBytes(buffer, offset + 36, MaxmiumVersions);
            LittleEndianConverter.GetBytes(buffer, offset + 40, VersionNumber);
            LittleEndianConverter.GetBytes(buffer, offset + 44, ClassId);

            // TODO: Get the actual NTFS Version in here to check against
            if (OwnerId != 0 || SecurityId != 0 || QuotaCharged != 0 || USN != 0)
            {
                LittleEndianConverter.GetBytes(buffer, offset + 48, OwnerId);
                LittleEndianConverter.GetBytes(buffer, offset + 52, SecurityId);
                LittleEndianConverter.GetBytes(buffer, offset + 56, QuotaCharged);
                LittleEndianConverter.GetBytes(buffer, offset + 64, USN);
            }
        }
    }
}