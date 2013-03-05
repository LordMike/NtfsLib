using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace NTFSLib.Objects.Security
{
    public class ACE
    {
        public ACEType Type { get; set; }
        public ACEFlags Flags { get; set; }
        public ushort Size { get; set; }
        public FileSystemRights AccessMask { get; set; }
        public SecurityIdentifier SID { get; set; }

        public static ACE ParseACE(byte[] data, int maxLength, int offset)
        {
			Debug.Assert(data.Length - offset >= maxLength);
			Debug.Assert(0 <= offset && offset <= data.Length);
            Debug.Assert(maxLength >= 8);

            ACE res = new ACE();

            res.Type = (ACEType)data[offset];
            res.Flags = (ACEFlags)data[offset + 1];
            res.Size = BitConverter.ToUInt16(data, offset + 2);
            res.AccessMask = (FileSystemRights) BitConverter.ToInt32(data, offset + 4);

            res.SID = new SecurityIdentifier(data,offset + 8);

            return res;
        }
    }
}