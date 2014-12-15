using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NtfsExtract.NTFS.Attributes;
using NtfsExtract.NTFS.Enums;
using NtfsExtract.NTFS.IO;
using NtfsExtract.NTFS.Objects;
using RawDiskLib;
using Attribute = NtfsExtract.NTFS.Attributes.Attribute;

namespace NtfsExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            char driveLetter = 'C';
            if (args.Length == 1)
                driveLetter = args[0][0];
            
            using (RawDisk disk = new RawDisk(driveLetter))
            {
                MftDiskExtents res = new MftDiskExtents();

                byte[] ntfsBoot = disk.ReadSectors(0, 1);
                BootSector boot = BootSector.ParseData(ntfsBoot, ntfsBoot.Length, 0);
                Console.WriteLine("MFT is at LCN " + boot.MFTCluster);

                MftDetails mft = GetMftDetails(disk, boot);
                Console.WriteLine("MFT is in " + mft.MftExtents.Length + " extents");

                res.Extents.AddRange(mft.MftExtents);

                using (RawDiskStream diskStream = disk.CreateDiskStream())
                using (NtfsDiskStream mftStream = new NtfsDiskStream(diskStream, false, mft.MftExtents, (uint)disk.ClusterSize, 0, (long)mft.MftSize))
                {
                    uint sectorsPrRecord = (uint)(boot.MFTRecordSizeBytes / disk.SectorSize);
                    ushort bytesPrSector = (ushort)disk.SectorSize;

                    int records = (int)(mftStream.Length / boot.MFTRecordSizeBytes);

                    byte[] tmp = new byte[boot.MFTRecordSizeBytes];
                    while (true)
                    {
                        int read = mftStream.Read(tmp, 0, tmp.Length);
                        if (read < boot.MFTRecordSizeBytes)
                        {
                            Console.WriteLine("Stopped reading as we got " + read + " bytes instead of " + tmp.Length + " bytes");
                            break;
                        }

                        FileRecord rec = FileRecord.Parse(tmp, 0, bytesPrSector, sectorsPrRecord);

                        // Keep track of all external extents to the MFT
                        RecordExternalDiskExtents(rec, res);

                        //// Special case for LIST attributes, since they can further reference more extents outside the MFT
                        //ProcessNonResidentListAttributes(disk, rec);
                    }
                }

                long clustersTotal = res.Extents.Sum(x => x.Clusters);

                Console.WriteLine("To copy: {0:N0} extents", res.Extents.Count);
                Console.WriteLine("{0:N0} clusters, {1:N0} bytes", clustersTotal, clustersTotal * disk.ClusterSize);
            }

            Console.ReadLine();
        }

        private static void RecordExternalDiskExtents(FileRecord rec, MftDiskExtents res)
        {
            foreach (Attribute attribute in rec.Attributes)
            {
                // Skip RESIDENT attributes
                if (attribute.NonResidentFlag == ResidentFlag.Resident)
                    continue;

                // Skip DATA attributes, if the MFT id is larger than 26 (first special reserved ID's)  Source: https://en.wikipedia.org/wiki/NTFS#Master_File_Table
                // Skip ID#8 DATA attribs, as they represent either the entire disk or unreadable clusters
                if (rec.FileReference.FileId == 8 || (rec.FileReference.FileId > 26 && attribute.Type == AttributeType.DATA))
                    continue;

                // Record external extents
                res.Extents.AddRange(attribute.NonResidentHeader.Fragments);

                //if (attribute.Type == AttributeType.INDEX_ALLOCATION)
                //    continue;

                //Console.WriteLine(" " + rec.FileReference + " : " + attribute.Type + "_" + attribute.AttributeName + " " + string.Join(",", attribute.NonResidentHeader.Fragments.Select(x => x.LCN + "->" + x.Clusters)));
            }
        }

        private static void ProcessNonResidentListAttributes(RawDisk disk, FileRecord rec)
        {
            // First, take all LIST attributes
            foreach (Attribute attrib in rec.Attributes)
            {
                if (attrib.Type != AttributeType.ATTRIBUTE_LIST || attrib.NonResidentFlag != ResidentFlag.NonResident)
                    continue;

                AttributeList list = (AttributeList)attrib;

                // Parse attributes from elsewhere on disk
                list.ParseAttributeNonResidentBody(disk);
            }
        }

        private static MftDetails GetMftDetails(RawDisk disk, BootSector boot)
        {
            byte[] mftFirst = disk.ReadClusters((long)boot.MFTCluster, 1);
            FileRecord rec = FileRecord.Parse(mftFirst, 0, (ushort)disk.SectorSize, (uint)(boot.MFTRecordSizeBytes / disk.SectorSize));

            // Get DATA attrib
            // TODO: Handle multiple DATA attributes
            AttributeGeneric dataAttrib = null;
            foreach (Attribute attribute in rec.Attributes)
            {
                if (attribute.Type == AttributeType.DATA && attribute.AttributeName.Length == 0)
                {
                    // Got it!
                    dataAttrib = attribute as AttributeGeneric;
                    break;
                }
            }

            Debug.Assert(dataAttrib != null);

            MftDetails res = new MftDetails();

            // Get attribute data
            // Parse out extents
            if (dataAttrib.NonResidentFlag == ResidentFlag.Resident)
            {
                byte[] dataAttribData = dataAttrib.Data;

                res.MftSize = dataAttrib.ResidentHeader.ContentLength;
                res.MftExtents = DataFragment.ParseFragments(dataAttribData, dataAttribData.Length, 0, 0, 0);
            }
            else
            {
                res.MftSize = dataAttrib.NonResidentHeader.ContentSize;
                res.MftExtents = dataAttrib.NonResidentHeader.Fragments;
            }

            Debug.Assert(res.MftExtents != null);

            return res;
        }
    }

    public class MftDiskExtents
    {
        public List<DataFragment> Extents { get; set; }

        public MftDiskExtents()
        {
            Extents = new List<DataFragment>();
        }
    }

    public class MftDetails
    {
        public ulong MftSize { get; set; }
        public DataFragment[] MftExtents { get; set; }


    }
}
