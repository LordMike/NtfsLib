using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NTFSLib.Helpers;
using NTFSLib.NTFS;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using RawDiskLib;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            const char driveLetter = 'E';
            RawDisk disk = new RawDisk(driveLetter);

            using (Stream stream = disk.CreateDiskStream())
            using (Stream streama = disk.CreateDiskStream())
            {
                NTFSParser parser = new NTFSParser(stream);
                NTFSParser parsera = new NTFSParser(streama);

                int longest = 0;
                foreach (FileRecord record in parser.GetRecords(true))
                {
                    int count = record.Attributes.Count;

                    if (count > longest)
                    {
                        longest = count;
                        Console.WriteLine(record.FileReference + " - " + count);
                    }
                }
            }

            NTFSDiskProvider provider = new NTFSDiskProvider(disk);

            NTFSWrapper ntfsWrapper = new NTFSWrapper(provider, 524288);
            ntfsWrapper.InitializeCommon();

            Console.WriteLine("Read NTFS. Version: " + ntfsWrapper.NTFSVersion);

            // Filerecord bitmap
            ntfsWrapper.ParseNonResidentAttribute(ntfsWrapper.FileMFT.Attributes.OfType<AttributeBitmap>().Single());
            BitArray bitmapData = ntfsWrapper.FileMFT.Attributes.OfType<AttributeBitmap>().Single().Bitfield;

            HashSet<AttributeType> types = new HashSet<AttributeType>();

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

                foreach (Attribute attribute in record.Attributes.Concat(record.ExternalAttributes).OrderBy(s => s.Id))
                {
                    bool wasNew = types.Add(attribute.Type);
                    if (wasNew)
                    {
                        File.AppendAllLines("out.txt", new[] { record.FileReference + ": " + attribute.Type });
                        Debugger.Break();
                    }

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

                Console.WriteLine();
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}