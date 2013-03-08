using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NTFSLib;
using NTFSLib.Helpers;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using RawDiskLib;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            const char driveLetter = 'C';
            RawDisk disk = new RawDisk(driveLetter);

            NTFSDiskProvider provider = new NTFSDiskProvider(disk);

            NTFS ntfs = new NTFS(provider);
            ntfs.InitializeCommon();

            Console.WriteLine("Read NTFS. Version: " + ntfs.NTFSVersion);

            // Read fragmented file
            for (uint i = 0; i < ntfs.FileRecordCount; i++)
            {
                FileRecord record = ntfs.ReadMFTRecord(i);

                if (!record.Flags.HasFlag(FileEntryFlags.FileInUse))
                    continue;

                ntfs.ParseAttributeLists(record);
                ntfs.ParseNonResidentAttributes(record);

                Console.WriteLine("Read {0:N0} of {1:N0}", i, ntfs.FileRecordCount);

                if (record.BaseFile.RawId != 0)
                    continue;

                if (!record.Attributes.OfType<AttributeFileName>().Any())
                    continue;

                string path = ntfs.BuildFileName(record, driveLetter);

                List<AttributeData> attributeData = record.Attributes.OfType<AttributeData>().Where(s => (s.NonResidentFlag == ResidentFlag.Resident && s.ResidentHeader.AttributeName == "") || (s.NonResidentFlag == ResidentFlag.NonResident && s.NonResidentHeader.AttributeName == "")).ToList();

                if (attributeData.Any(s => s.NonResidentFlag == ResidentFlag.Resident))
                    continue;

                if (attributeData.SelectMany(s => s.DataFragments).Count() <= 1)
                    continue;

                if (attributeData.First().NonResidentHeader.ContentSize > 256000000)
                    continue;

                if (attributeData.First().NonResidentHeader.Compression != 0)
                    continue;

                // Hash files
                try
                {
                    //string sss = "0x" + BitConverter.ToString(record.Attributes.OfType<AttributeData>().First().NonResidentHeader.xxxx).Replace("-", ", 0x");

                    //var extents = record.Attributes.OfType<AttributeData>().First().NonResidentHeader.Fragments;

                    //string ss = "";
                    //for (int kk = 0; kk < extents.Length; kk++)
                    //{
                    //    ss += string.Format("{0}: {1} -> {2} ({3} clusters); VCN: {4}\n", kk, extents[kk].LCN, extents[kk].LCN + extents[kk].Clusters, extents[kk].Clusters, extents[kk].StartingVCN);
                    //    Console.WriteLine("{0}: {1} -> {2} ({3} clusters); VCN: {4}", kk, extents[kk].LCN, extents[kk].LCN + extents[kk].Clusters, extents[kk].Clusters, extents[kk].StartingVCN);
                    //}

                    // Hash the file
                    Console.WriteLine("Hashing {0}!", path);
                    MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();

                    byte[] hashDiskIo;
                    byte[] dataDiskIo;
                    using (Stream stream = File.OpenRead(path))
                    {
                        dataDiskIo = new byte[stream.Length];
                        stream.Read(dataDiskIo, 0, dataDiskIo.Length);
                        stream.Position = 0;

                        hashDiskIo = x.ComputeHash(stream);
                    }

                    byte[] hashNtfs;
                    byte[] dataRaw;
                    using (Stream stream = ntfs.OpenFileRecord(record))
                    {
                        dataRaw = new byte[stream.Length];
                        stream.Read(dataRaw, 0, dataRaw.Length);
                        stream.Position = 0;

                        hashNtfs = x.ComputeHash(stream);
                    }

                    //File.WriteAllBytes("a.bin", dataRaw);
                    //File.WriteAllBytes("b.bin", dataDiskIo);

                    //byte[] data = ntfs.ReadMFTRecordData(i);
                    //byte[] xx = data.Skip(record.OffsetToFirstAttribute + record.Attributes[0].TotalLength + record.Attributes[1].TotalLength + record.Attributes[2].NonResidentHeader.ListOffset).Take(record.Attributes[2].TotalLength - record.Attributes[2].NonResidentHeader.ListOffset).ToArray();
                    //string xxx = "0x" + BitConverter.ToString(xx).Replace("-", ", 0x");

                    for (int j = 0; j < dataRaw.Length; j++)
                    {
                        if (dataRaw[j] != dataDiskIo[j])
                        {
                            Console.WriteLine("Diff at byte {0:N0} of {1:N0}!", j, dataRaw.Length);
                        }
                    }

                    if (hashNtfs.SequenceEqual(hashDiskIo))
                        Console.WriteLine("Success!");
                    else
                        Console.WriteLine("Error!");
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed");
                }
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
