using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NTFSLib.Objects
{
    public class DataFragment
    {
        public byte Size { get; set; }              // Todo: Rename
        public long ClusterCount { get; set; }     // Todo: Rename
        public byte CompressedClusters { get; set; }
        public long LCN { get; set; }
        public long StartingVCN { get; set; }

        /// <summary>
        /// If this fragment is sparse, there is not data on the disk to reflect the data in the file. 
        /// This chunk is therefore all zeroes.
        /// </summary>
        public bool IsSparseFragment
        {
            get { return LCN == 0; }
        }

        public bool IsCompressed
        {
            get { return CompressedClusters != 0; }
        }

        public int ThisObjectLength { get; private set; }

        public static DataFragment ParseData(byte[] data, long previousLcn, int offset)
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

            offsetBytes = (byte)(offsetBytes & 0xF7);    // 0xF7: 1111 0111
            Debug.Assert(countBytes <= 8, "Fragment metadata exceeded 8 bytes");
            Debug.Assert(offsetBytes <= 8, "Fragment metadata exceeded 8 bytes");

            byte[] tmpData = new byte[8];
            Array.Copy(data, offset + 1, tmpData, 0, countBytes);

            res.ClusterCount = BitConverter.ToInt64(tmpData, 0);
            res.LCN = previousLcn;

            if (offsetBytes == 0)
            {
                // Sparse chunk
                res.LCN = 0;
            }
            else
            {
                long deltaLcn = 0;

                for (int i = offsetBytes - 1; i >= 0; i--)
                {
                    deltaLcn = deltaLcn << 8;
                    deltaLcn += data[offset + 1 + countBytes + i];
                }

                // Is negative?
                long negativeValue = (long)128 << 8 * (offsetBytes - 1);
                if ((deltaLcn & negativeValue) == negativeValue)
                {
                    // Negtive
                    // Set the remaining bytes to 0xFF
                    long tmp = 0xFF;
                    for (int i = 0; i < 8 - offsetBytes; i++)
                    {
                        tmp = tmp << 8;
                        tmp |= 0xFF;
                    }

                    for (int i = 8 - offsetBytes; i < 8; i++)
                    {
                        tmp = tmp << 8;
                    }

                    deltaLcn |= tmp;
                }

                res.LCN = res.LCN + deltaLcn;
            }

            return res;
        }

        public static DataFragment[] ParseFragments(byte[] data, int maxLength, int offset, long startingVCN, long endingVCN)
        {
            Debug.Assert(data.Length - offset >= maxLength);
            Debug.Assert(0 <= offset && offset <= data.Length);

            List<DataFragment> fragments = new List<DataFragment>();

            long vcn = startingVCN;

            int pointer = offset;
            long lastLcn = 0;
            while (pointer <= offset + maxLength)
            {
                Debug.Assert(pointer <= offset + maxLength);

                DataFragment fragment = ParseData(data, lastLcn, pointer);

                pointer += fragment.ThisObjectLength;

                if (fragment.Size == 0)
                    // Last fragment
                    break;

                fragment.StartingVCN = vcn;

                vcn += fragment.ClusterCount;

                if (!fragment.IsSparseFragment)
                    // Don't count sparse fragments for offsets
                    lastLcn = fragment.LCN;

                fragments.Add(fragment);
            }

            // Checks
            Debug.Assert(fragments.Count == 0 || startingVCN == fragments[0].StartingVCN);
            Debug.Assert(endingVCN == vcn - 1);

            // Compact compressed fragments
            for (int i = 0; i < fragments.Count; i++)
            {
                if (fragments.Count > i + 1 &&
                    (fragments[i].ClusterCount + fragments[i + 1].ClusterCount) % 16 == 0 &&
                    fragments[i + 1].ClusterCount < 16)
                {
                    // Compact
                    fragments[i].CompressedClusters = (byte)fragments[i + 1].ClusterCount;
                    fragments.RemoveAt(i + 1);

                    i--;
                }
            }

            // Return
            return fragments.ToArray();
        }
    }
}