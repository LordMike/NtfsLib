using System;
using System.Diagnostics;

namespace NTFSLib.Objects
{
    public class DataFragment
    {
        public byte Size { get; set; }
        public ulong ClusterCount { get; set; }
        public ulong LCN { get; set; }
        public ulong StartingVCN { get; set; }

        /// <summary>
        /// If this fragment is sparse, there is not data on the disk to reflect the data in the file. 
        /// This chunk is therefore all zeroes.
        /// </summary>
        public bool IsSparseFragment
        {
            get { return LCN == 0; }
        }

        public int ThisObjectLength { get; private set; }

        public static DataFragment ParseData(byte[] data, int offset = 0)
        {
            DataFragment res = new DataFragment();

            res.Size = data[offset];
            byte offsetBytes = (byte)(res.Size >> 4);
            byte countBytes = (byte)(res.Size & 0x0F);

            res.ThisObjectLength = 1 + countBytes + offsetBytes;

            if (countBytes == 0)
            {
                res.Size = 0;
                return res;
            }

            Debug.Assert(countBytes <= 8, "Fragment metadata exceeded 8 bytes");
            Debug.Assert(offsetBytes <= 8, "Fragment metadata exceeded 8 bytes");

            byte[] tmpData = new byte[16];
            Array.Copy(data, offset + 1, tmpData, 0, countBytes);

            res.ClusterCount = BitConverter.ToUInt64(tmpData, 0);

            if (offsetBytes == 0)
            {
                // Sparse chunk
                res.LCN = 0;
            }
            else
            {
                // Data chunk
                Array.Clear(tmpData, 0, tmpData.Length);
                Array.Copy(data, offset + 1 + countBytes, tmpData, 0, offsetBytes);
                res.LCN = BitConverter.ToUInt64(tmpData, 0);
            }

            return res;
        }
    }
}