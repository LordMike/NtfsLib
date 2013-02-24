using System;
using System.Diagnostics;
using System.Text;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects
{
    public class ExtendedAttribute
    {
        public uint Size { get; set; }
        public MFTEAFlags EAFlag { get; set; }
        public byte NameLength { get; set; }
        public ushort ValueLength { get; set; }
        public string Name { get; set; }
        public byte[] Value { get; set; }

        public static ExtendedAttribute ParseData(byte[] data, int maxLength, int offset)
        {
            Debug.Assert(maxLength >= 8);

            ExtendedAttribute res = new ExtendedAttribute();

            res.Size = BitConverter.ToUInt32(data, offset);
            res.EAFlag = (MFTEAFlags)data[offset + 4];
            res.NameLength = data[offset + 5];
            res.ValueLength = BitConverter.ToUInt16(data, offset + 6);

            res.Name = Encoding.Unicode.GetString(data, offset + 8, res.NameLength);
            res.Value = new byte[res.ValueLength];
            Array.Copy(data, offset + 8 + res.NameLength, res.Value, 0, res.ValueLength);

            return res;
        }
    }
}