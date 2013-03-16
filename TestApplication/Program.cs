using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NTFSLib.Helpers;
using NTFSLib.NTFS;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Specials.Files;
using RawDiskLib;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            //{
            //    DirectoryInfo dir = new DirectoryInfo("E:\\testDir");

            //    dir.Create();
            //    for (int i = 1024000; i < 1024000 * 2; i++)
            //    {
            //        File.Create(Path.Combine(dir.FullName, i + ".txt"));
            //        if (i % 10000 == 0)
            //            Console.WriteLine(i);
            //    }
            //}

            //Console.WriteLine("Done");
            //Console.ReadLine();

            const char driveLetter = 'E';
            RawDisk disk = new RawDisk(driveLetter);

            NTFSDiskProvider provider = new NTFSDiskProvider(disk);

            NTFSWrapper ntfsWrapper = new NTFSWrapper(provider, 524288);
            ntfsWrapper.InitializeCommon();

            Console.WriteLine("Read NTFS. Version: " + ntfsWrapper.NTFSVersion);

            //// Read sparse file
            //{
            //    // C:\Users\Michael\Desktop\TestSparse.txt
            //    var x1 = ntfs.GetRootDirectory();
            //    var x2 = x1.ListDirectories(false).Single(s => s.Name == "Users");
            //    var x3 = x2.ListDirectories(false).Single(s => s.Name == "Michael");
            //    var x4 = x3.ListDirectories(false).Single(s => s.Name == "Desktop");
            //    var x5 = x4.ListFiles(false).Single(s => s.Name == "TestSparse.txt");

            //    Console.WriteLine(x5.MFTRecord.FileReference);

            //    var strm = x5.OpenRead();
            //    byte[] data = new byte[strm.Length];
            //    strm.Read(data, 0, data.Length);
            //}

            //// Read compressed file
            //{
            //    // C:\Users\Michael\Desktop\TestDoc.txt
            //    var x1 = ntfs.GetRootDirectory();
            //    var x2 = x1.ListDirectories(false).Single(s => s.Name == "Users");
            //    var x3 = x2.ListDirectories(false).Single(s => s.Name == "Michael");
            //    var x4 = x3.ListDirectories(false).Single(s => s.Name == "Desktop");
            //    var x5 = x4.ListFiles(false).Single(s => s.Name == "TestDoc.txt");

            //    var xx = x5.MFTRecord.Attributes.OfType<AttributeData>().Single();

            //    Console.WriteLine(x5.MFTRecord.FileReference);

            //    var strm = x5.OpenRead();
            //    byte[] data = new byte[strm.Length - 100];
            //    strm.Position = 100;
            //    strm.Read(data, 0, data.Length);
            //}

            //// Read compressed-sparse file
            //{
            //    // C:\Users\Michael\Desktop\TestSparse - copy.txt
            //    var x1 = ntfs.GetRootDirectory();
            //    var x2 = x1.ListDirectories(false).Single(s => s.Name == "Users");
            //    var x3 = x2.ListDirectories(false).Single(s => s.Name == "Michael");
            //    var x4 = x3.ListDirectories(false).Single(s => s.Name == "Desktop");
            //    var x5 = x4.ListFiles(false).Single(s => s.Name == "TestSparse - Copy.txt");

            //    var xx = x5.MFTRecord.Attributes.OfType<AttributeData>().Single();

            //    Console.WriteLine(x5.MFTRecord.FileReference);

            //    var strm = x5.OpenRead();
            //    byte[] data = new byte[strm.Length - 100];
            //    strm.Position = 100;
            //    strm.Read(data, 0, data.Length);
            //}

            //// Iterate dirs
            //NtfsDirectory dir = ntfs.GetRootDirectory();
            //Queue<NtfsDirectory> dirs = new Queue<NtfsDirectory>();
            //dirs.Enqueue(dir);

            //while (dirs.Count > 0)
            //{
            //    NtfsDirectory currDir = dirs.Dequeue();

            //    int files = currDir.ListFiles().Count();

            //    Console.WriteLine(files + ": " + ntfs.BuildFileName(currDir.MFTRecord, driveLetter));

            //    foreach (NtfsDirectory subDir in currDir.ListDirectories())
            //    {
            //        if (subDir.MFTRecord.FileReference.FileId == (uint)SpecialMFTFiles.RootDir)
            //            continue;

            //        dirs.Enqueue(subDir);
            //    }
            //}

            // Parse $AttrDef
            AttrDef attrDef = AttrDef.ParseFile(ntfsWrapper.OpenFileRecord(ntfsWrapper.FileAttrDef));

            // Parse $Secure
            //var xy = ntfs.OpenFileRecord(ntfs.FileSecure, "$SDS");
            //ntfs.ParseNonResidentAttributes(ntfs.FileSecure);

            //byte[] data = new byte[xy.Length];
            //xy.Read(data, 0, data.Length);

            //Secure sss = Secure.ParseFile(ntfs.OpenFileRecord(ntfs.FileSecure, "$SDS"));

            //public FileRecord FileSecure { get; private set; }
            //public FileRecord FileLogFile { get; private set; }
            //public FileRecord FileVolume { get; private set; }
            //public FileRecord FileRootDir { get; private set; }
            //public FileRecord FileBitmap { get; private set; }
            //public FileRecord FileBoot { get; private set; }
            //public FileRecord FileBadClus { get; private set; }
            //public FileRecord FileUpCase { get; private set; }
            //public FileRecord FileExtend { get; private set; }

            // Read E:\testDir\
            //ntfs.ParseNonResidentAttributes(ntfs.FileRootDir);
            //var x1 = ntfs.FileRootDir.Attributes.OfType<AttributeIndexAllocation>().First();
            //var x2 = ntfs.ReadMFTRecord((uint)x1.Entries.First(s => s.ChildFileName.FileName == "testDir").FileRefence.FileId);
            //ntfs.ParseAttributeLists(x2);
            //ntfs.ParseNonResidentAttributes(x2);

            //foreach (AttributeIndexAllocation attributeIndexAllocation in x2.Attributes.OfType<AttributeIndexAllocation>())
            //{
            //    Console.WriteLine(attributeIndexAllocation.Entries.Length);
            //}

            //Console.WriteLine(x2.Attributes.OfType<AttributeIndexRoot>().First().Entries.Length);

            // Filerecord bitmap
            ntfsWrapper.ParseNonResidentAttribute(ntfsWrapper.FileMFT.Attributes.OfType<AttributeBitmap>().Single());
            BitArray bitmapData = ntfsWrapper.FileMFT.Attributes.OfType<AttributeBitmap>().Single().Bitfield;

            // Read fragmented file
            for (uint i = 0; i < ntfsWrapper.FileRecordCount; i++)
            {
                if (!ntfsWrapper.InRawDiskCache(i))
                    ntfsWrapper.PrepRawDiskCache(i);

                if (!bitmapData[(int)i])
                    continue;

                FileRecord record = ntfsWrapper.ReadMFTRecord(i);

                if (record.Flags.HasFlag(FileEntryFlags.FileInUse))
                    ntfsWrapper.ParseNonResidentAttributes(record);

                Console.Write("Read {0:N0} of {1:N0} - ({2:N0} bytes {3:N0} allocated)", i, ntfsWrapper.FileRecordCount, record.SizeOfFileRecord, record.SizeOfFileRecordAllocated);

                if (record.Flags.HasFlag(FileEntryFlags.FileInUse))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write(" (InUse)");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write(" (Not InUse)");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (bitmapData[(int)i])
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write(" (Bitmap:InUse)");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write(" (Bitmap:Not InUse)");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (record.Flags.HasFlag(FileEntryFlags.Directory))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(" (dir)");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (record.BaseFile.FileId != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(" (base: {0})", record.BaseFile);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (Enum.IsDefined(typeof(SpecialMFTFiles), record.FileReference.FileId))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(" ({0})", (SpecialMFTFiles)record.FileReference.FileId);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.WriteLine();

                foreach (Attribute attribute in record.Attributes.OrderBy(s => s.Id))
                {
                    string name = string.IsNullOrWhiteSpace(attribute.AttributeName) ? string.Empty : " '" + attribute.AttributeName + "'";

                    Console.Write("  " + attribute.Id + " (" + attribute.Type);

                    if (name != string.Empty)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(name);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    Console.Write(")");

                    AttributeFileName attributeFileName = attribute as AttributeFileName;
                    if (attributeFileName != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(" '{0}'", attributeFileName.FileName);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    AttributeData attributeData = attribute as AttributeData;
                    if (attributeData != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(" {0}", attributeData.NonResidentFlag);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" ({0:N0} bytes)", attributeData.NonResidentFlag == ResidentFlag.NonResident ? attributeData.NonResidentHeader.ContentSize : (ulong)attributeData.DataBytes.Length);

                        if (attributeData.NonResidentFlag == ResidentFlag.Resident)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(" ('{0}')", Encoding.ASCII.GetString(attributeData.DataBytes, 0, Math.Min(attributeData.DataBytes.Length, 30)));
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine();

                            foreach (DataFragment fragment in attributeData.DataFragments)
                            {
                                Console.Write("    LCN: {0:N0} ({1:N0} clusters) ", fragment.LCN, fragment.Clusters);

                                if (fragment.IsCompressed)
                                {
                                    Console.ForegroundColor = ConsoleColor.Blue;
                                    Console.Write(" (Compressed)");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                }

                                if (fragment.IsSparseFragment)
                                {
                                    Console.ForegroundColor = ConsoleColor.Blue;
                                    Console.Write(" (Sparse)");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                }

                                Console.WriteLine();
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    Console.WriteLine();
                }

                List<DataFragment> frags = record.Attributes.OfType<AttributeData>().Where(s => s.AttributeName == string.Empty && s.NonResidentFlag == ResidentFlag.NonResident).SelectMany(s => s.DataFragments).OrderBy(s => s.StartingVCN).ToList();
                DataFragment.CompactFragmentList(frags);

                if (frags.Count > 7)
                {
                    using (var sw = new StreamWriter(i + ".csv"))
                    {
                        foreach (var frag in frags)
                        {
                            sw.WriteLine("{0};{1};{2};{3}", frag.LCN, frag.Clusters, frag.IsSparseFragment, frag.IsCompressed);
                        }
                    }
                    Console.ReadLine();
                }

                Console.ReadLine();
                Console.WriteLine();
                //HashFile(ntfs,record,driveLetter);
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static void HashFile(NTFSWrapper ntfsWrapper, FileRecord record, char driveLetter)
        {
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
                string path = ntfsWrapper.BuildFileName(record, driveLetter);

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
                using (Stream stream = ntfsWrapper.OpenFileRecord(record))
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

                bool equal = true;
                if (dataRaw.Length != dataDiskIo.Length)
                {
                    Console.WriteLine("Diff at length {0:N0} and {1:N0}!", dataRaw.Length, dataDiskIo.Length);
                    equal = false;
                }
                else
                {
                    for (int j = 0; j < dataRaw.Length; j++)
                    {
                        if (dataRaw[j] != dataDiskIo[j])
                        {
                            Console.WriteLine("Diff at byte {0:N0} of {1:N0}!", j, dataRaw.Length);
                            equal = false;
                            break;
                        }
                    }
                }

                if (equal)
                    Console.WriteLine("Success!");
                else
                {
                    Console.WriteLine("Error!");
                    Console.ReadLine();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed");
            }
        }
    }
}
