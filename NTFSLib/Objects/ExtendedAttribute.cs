using System;
using System.Diagnostics;
using System.Text;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects
{
    public class ExtendedAttribute
    {
        public int Size { get; set; }
        public MFTEAFlags EAFlag { get; set; }
        public byte NameLength { get; set; }
        public ushort ValueLength { get; set; }
        public string Name { get; set; }
        public byte[] Value { get; set; }

        public static int GetSize(byte[] data, int offset)
        {
            return BitConverter.ToInt32(data, offset);
        }

        public static ExtendedAttribute ParseData(byte[] data, int maxLength, int offset)
        {
            Debug.Assert(maxLength >= 8);

            ExtendedAttribute res = new ExtendedAttribute();

            res.Size = BitConverter.ToInt32(data, offset);
            res.EAFlag = (MFTEAFlags)data[offset + 4];
            res.NameLength = data[offset + 5];
            res.ValueLength = BitConverter.ToUInt16(data, offset + 6);

            Debug.Assert(res.Size <= maxLength);
            Debug.Assert(res.NameLength <= res.Size);
            Debug.Assert(res.ValueLength <= res.Size);

            res.Name = Encoding.ASCII.GetString(data, offset + 8, res.NameLength);
            res.Value = new byte[res.ValueLength];
            Array.Copy(data, offset + 8 + res.NameLength, res.Value, 0, res.ValueLength);

            return res;
        }
    }
}