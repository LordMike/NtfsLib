using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NTFSLib.Objects
{
    public class DataFragment
    {
        /// <summary>
        /// Note. This is not the size in bytes of this fragment. It is a very compact NTFS-specific way of writing lengths.
        /// Be warned when using it.
        /// </summary>
        public byte FragmentSizeBytes { get; set; }
        public long Clusters { get; set; }
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

        /// <summary>
        /// If this fragment is compressed, it will contain some clusters on disk (The Clusters property) which contain actual (compressed) data.
        /// After that, there is a number of clusters (CompressedClusters property) which act as 'fillers' for the decompressed data. It does not exist on disk.
        /// </summary>
        public bool IsCompressed
        {
            get { return CompressedClusters != 0; }
        }

        public int ThisObjectLength { get; private set; }

        public static DataFragment ParseData(byte[] data, long previousLcn, int offset)
        {
            DataFragment res = new DataFragment();

            res.FragmentSizeBytes = data[offset];
            byte offsetBytes = (byte)(res.FragmentSizeBytes >> 4);
            byte countBytes = (byte)(res.FragmentSizeBytes & 0x0F);

            res.ThisObjectLength = 1 + countBytes + offsetBytes;

            if (countBytes == 0)
            {
                res.FragmentSizeBytes = 0;
                return res;
            }

            offsetBytes = (byte)(offsetBytes & 0xF7);    // 0xF7: 1111 0111
            Debug.Assert(countBytes <= 8, "Fragment metadata exceeded 8 bytes");
            Debug.Assert(offsetBytes <= 8, "Fragment metadata exceeded 8 bytes");

            byte[] tmpData = new byte[8];
            Array.Copy(data, offset + 1, tmpData, 0, countBytes);

            res.Clusters = BitConverter.ToInt64(tmpData, 0);
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

                if (fragment.FragmentSizeBytes == 0)
                    // Last fragment
                    break;

                fragment.StartingVCN = vcn;

                vcn += fragment.Clusters;

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
                    (fragments[i].Clusters + fragments[i + 1].Clusters) % 16 == 0 &&
                    fragments[i + 1].Clusters < 16)
                {
                    // Compact
                    fragments[i].CompressedClusters = (byte)fragments[i + 1].Clusters;
                    fragments.RemoveAt(i + 1);

                    i--;
                }
            }

            // Return
            return fragments.ToArray();
        }
    }
}