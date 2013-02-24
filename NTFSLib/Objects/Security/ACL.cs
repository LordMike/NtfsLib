using System;
using System.Diagnostics;

namespace NTFSLib.Objects.Security
{
    public class ACL
    {
        public byte ACLRevision { get; set; }
        public ushort ACLSize { get; set; }
        public ushort ACECount { get; set; }
        public ACE[] ACEs { get; set; }

        public static ACL ParseACL(byte[] data, int maxLength, int offset)
        {
			Debug.Assert(data.Length - offset >= maxLength);
			Debug.Assert(0 <= offset && offset <= data.Length);
            Debug.Assert(maxLength >= 8);

            ACL res = new ACL();

            res.ACLRevision = data[offset];
            res.ACLSize = BitConverter.ToUInt16(data, offset + 2);
            res.ACECount = BitConverter.ToUInt16(data, offset + 4);

            res.ACEs = new ACE[res.ACECount];

            int pointer = offset + 8;
            for (int i = 0; i < res.ACECount; i++)
            {
                ACE ace = ACE.ParseACE(data, res.ACLSize, pointer);
                res.ACEs[i] = ace;

                pointer += ace.Size;
            }

            return res;
        }
    }
}