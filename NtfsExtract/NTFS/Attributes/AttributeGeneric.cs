using System;
using NtfsExtract.NTFS.Enums;
using NtfsExtract.NTFS.IO;
using RawDiskLib;

namespace NtfsExtract.NTFS.Attributes
{
    public class AttributeGeneric : Attribute
    {
        public byte[] Data { get; set; }

        public override AttributeResidentAllow AllowedResidentStates
        {
            get
            {
                return AttributeResidentAllow.Resident | AttributeResidentAllow.NonResident;
            }
        }

        internal override void ParseAttributeResidentBody(byte[] data, int maxLength, int offset)
        {
            base.ParseAttributeResidentBody(data, maxLength, offset);

            // Get data
            Data = new byte[maxLength];
            Array.Copy(data, offset, Data, 0, maxLength);
        }

        internal override void ParseAttributeNonResidentBody(RawDisk disk)
        {
            base.ParseAttributeNonResidentBody(disk);

            // Read clusters from disk
            Data = new byte[NonResidentHeader.ContentSize];

            using (RawDiskStream diskStream = disk.CreateDiskStream())
            using (NtfsDiskStream attribStream = new NtfsDiskStream(diskStream, false, NonResidentHeader.Fragments, (uint)disk.ClusterSize, 0, Data.LongLength))
                attribStream.Read(Data, 0, Data.Length);
        }

        public override string ToString()
        {
            return "ATTRIB_" + Type + "_" + NonResidentFlag + "_" + (AttributeName == string.Empty ? "<noname>" : AttributeName);
        }
    }
}