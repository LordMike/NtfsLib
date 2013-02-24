using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Security;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeSecurityDescriptor : Attribute
    {
        public byte Revision { get; set; }
        public ControlFlags ControlFlags { get; set; }
        public uint OffsetToUserSID { get; set; }
        public uint OffsetToGroupSID { get; set; }
        public uint OffsetToSACL { get; set; }
        public uint OffsetToDACL { get; set; }

        public ACL SACL { get; set; }
        public ACL DACL { get; set; }
        public SecurityIdentifier UserSID { get; set; }
        public SecurityIdentifier GroupSID { get; set; }

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

            Debug.Assert(maxLength >= 20);

            Revision = data[offset];
            ControlFlags = (ControlFlags)BitConverter.ToUInt16(data, offset + 2);
            OffsetToUserSID = BitConverter.ToUInt32(data, offset + 4);
            OffsetToGroupSID = BitConverter.ToUInt32(data, offset + 8);
            OffsetToSACL = BitConverter.ToUInt32(data, offset + 12);
            OffsetToDACL = BitConverter.ToUInt32(data, offset + 16);

            if (OffsetToUserSID != 0)
                UserSID = new SecurityIdentifier(data, offset + (int)OffsetToUserSID);
            if (OffsetToGroupSID != 0)
                GroupSID = new SecurityIdentifier(data, offset + (int)OffsetToGroupSID);

            if (OffsetToSACL != 0 && ControlFlags.HasFlag(ControlFlags.SystemAclPresent))
            {
                SACL = ACL.ParseACL(data, 8, (int)(offset + OffsetToSACL));
            }
            if (OffsetToDACL != 0 && ControlFlags.HasFlag(ControlFlags.DiscretionaryAclPresent))
            {
                DACL = ACL.ParseACL(data, 8, (int)(offset + OffsetToDACL));
            }
        }
    }
}