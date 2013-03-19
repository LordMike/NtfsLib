using System;
using System.Diagnostics;
using NTFSLib.Utilities;

namespace NTFSLib.Objects.Headers
{
    public class AttributeNonResidentHeader : ISaveableObject
    {
        public long StartingVCN { get; set; }
        public long EndingVCN { get; set; }
        public ushort ListOffset { get; set; }
        public ushort CompressionUnitSize { get; set; }
        public ulong ContentSizeAllocated { get; set; }
        public ulong ContentSize { get; set; }
        public ulong ContentSizeInitialized { get; set; }
        public ulong ContentSizeCompressed { get; set; }
        public DataFragment[] Fragments { get; set; }

        public static AttributeNonResidentHeader ParseHeader(byte[] data, int offset = 0)
        {
            Debug.Assert(data.Length - offset >= 48);
            Debug.Assert(offset >= 0);

            AttributeNonResidentHeader res = new AttributeNonResidentHeader();

            res.StartingVCN = BitConverter.ToInt64(data, offset);
            res.EndingVCN = BitConverter.ToInt64(data, offset + 8);
            res.ListOffset = BitConverter.ToUInt16(data, offset + 16);
            res.CompressionUnitSize = BitConverter.ToUInt16(data, offset + 18);
            res.ContentSizeAllocated = BitConverter.ToUInt64(data, offset + 24);
            res.ContentSize = BitConverter.ToUInt64(data, offset + 32);
            res.ContentSizeInitialized = BitConverter.ToUInt64(data, offset + 40);

            if (res.CompressionUnitSize != 0)
            {
                Debug.Assert(data.Length - offset >= 56);
                res.ContentSizeCompressed = BitConverter.ToUInt64(data, offset + 48);
            }

            return res;
        }

        public int GetSaveLength()
        {
            int size = ContentSizeCompressed != 0 ? 56 : 48;

            if (Fragments != null)
            {
                size += DataFragment.GetSaveLength(Fragments);
            }

            return size;
        }

        public void Save(byte[] buffer, int offset)
        {
            Debug.Assert(buffer.Length - offset >= GetSaveLength());

            LittleEndianConverter.GetBytes(buffer, offset, StartingVCN);
            LittleEndianConverter.GetBytes(buffer, offset + 8, EndingVCN);
            LittleEndianConverter.GetBytes(buffer, offset + 16, ListOffset);
            LittleEndianConverter.GetBytes(buffer, offset + 18, CompressionUnitSize);
            LittleEndianConverter.GetBytes(buffer, offset + 24, ContentSizeAllocated);
            LittleEndianConverter.GetBytes(buffer, offset + 32, ContentSize);
            LittleEndianConverter.GetBytes(buffer, offset + 40, ContentSizeInitialized);

            if (CompressionUnitSize != 0)
                LittleEndianConverter.GetBytes(buffer, offset + 48, ContentSizeCompressed);

            if (Fragments != null)
            {
                int pointer = ContentSizeCompressed != 0 ? 56 : 48;

                DataFragment.Save(buffer, pointer, Fragments);
            }
        }
    }
}