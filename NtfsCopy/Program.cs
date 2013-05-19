using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NTFSLib.Helpers;
using NTFSLib.IO;
using NTFSLib.NTFS;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using RawDiskLib;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NtfsCopy
{
    class Program
    {
        static void Main(string[] args)
        {
            Options opts = new Options();

            bool success = opts.Parse(args);

            if (!success)
            {
                AwesomeConsole.WriteLine("Unable to parse the commandline:");
                PrintError(opts.ErrorDetails);
                AwesomeConsole.WriteLine();
                AwesomeConsole.WriteLine("Try --help for more informations");
                return;
            }

            if (opts.ActionType == ActionType.ShowHelp)
            {
                opts.DisplayHelp();
                return;
            }

            uint mftId = 0;

            if (opts.SourceType == PathType.File)
                mftId = FindPathMftId(opts);
            else if (opts.SourceType == PathType.MftId)
                mftId = opts.MftId;

            using (RawDisk disk = new RawDisk(opts.Drive))
            {
                NTFSWrapper wrapper = new NTFSWrapper(new NTFSDiskProvider(disk), 1);

                if (wrapper.FileRecordCount < mftId)
                {
                    PrintError("The given path or MftID didn't exist");
                    AwesomeConsole.WriteLine();

                    switch (opts.SourceType)
                    {
                        case PathType.File:
                            PrintError("Specified " + opts.SourceType + ": " + opts.Source);
                            break;
                        case PathType.MftId:
                            PrintError("Specified type: MftId, id: " + opts.MftId);
                            break;
                    }

                    AwesomeConsole.WriteLine();

                    return;
                }

                PerformCopy(wrapper, opts, mftId);
            }

            if (Debugger.IsAttached)
                Console.ReadLine();
        }

        private static void PerformCopy(NTFSWrapper wrapper, Options opts, uint mftId)
        {
            // Fetch record
            FileRecord record = wrapper.ReadMFTRecord(mftId);

            // Find attribute(s)
            List<Attribute> attribs = record.Attributes.Concat(record.ExternalAttributes).Where(s => s.Type == opts.SourceAttribute && s.AttributeName == opts.SourceName).ToList();

            if (!attribs.Any())
            {
                PrintError("Unable to find any attribute named \"" + opts.SourceName + "\" of type " + opts.SourceAttribute);
                AwesomeConsole.WriteLine();
                return;
            }

            // Determine resident or non-resident data
            Stream fileStream = null;
            if (attribs.All(x => x.NonResidentFlag == ResidentFlag.NonResident))
            {
                // Fetch fragments
                DataFragment[] fragments = attribs.SelectMany(s => s.NonResidentHeader.Fragments).OrderBy(s => s.StartingVCN).ToArray();

                ushort compressionUnitSize = attribs[0].NonResidentHeader.CompressionUnitSize;
                ushort compressionClusterCount = (ushort)(compressionUnitSize == 0 ? 0 : Math.Pow(2, compressionUnitSize));

                fileStream = new NtfsDiskStream(wrapper.GetDiskStream(), true, fragments, wrapper.BytesPrCluster, compressionClusterCount, (long)attribs.First().NonResidentHeader.ContentSize);
            }
            else
            {
                // Fetch data
                if (attribs.Count != 1)
                {
                    PrintError("There were multiple attributes for this single file that matched, yet they were all Resident. This is an error.");
                    AwesomeConsole.WriteLine();
                    return;
                }

                Attribute attrib = attribs.First();

                if (attrib is AttributeGeneric)
                {
                    AttributeGeneric generic = (AttributeGeneric)attrib;

                    fileStream = new MemoryStream(generic.Data);
                }
                else if (attrib is AttributeData)
                {
                    AttributeData data = (AttributeData)attrib;

                    fileStream = new MemoryStream(data.DataBytes);
                }
                else
                {
                    PrintError("Only certain resident attributes are supported, like $DATA");
                    AwesomeConsole.WriteLine();
                    return;
                }
            }

            // Perform copy
            using (AwesomeConsole.BeginSequentialWrite())
            {
                AwesomeConsole.Write("Found data, copying ");
                AwesomeConsole.Write(fileStream.Length.ToString("N0"), ConsoleColor.Green);
                AwesomeConsole.Write(" bytes to ");
                AwesomeConsole.WriteLine(opts.Destination, ConsoleColor.Green);
            }

            using (FileStream fs = File.OpenWrite(opts.Destination))
            {
                if (fs.CanSeek && fs.CanWrite)
                    // Pre-expand the destination, to help filesystems allocate files
                    fs.SetLength(fileStream.Length);

                byte[] buff = new byte[65535];
                int lastProgressed = -1;
                for (long offset = 0; offset < fileStream.Length; offset += buff.Length)
                {
                    int read = fileStream.Read(buff, 0, buff.Length);

                    if (read == 0)
                        // Finished
                        break;

                    fs.Write(buff, 0, read);

                    int progressed = (int)((offset * 1f / fileStream.Length) * 20);
                    if (read != buff.Length)
                        // Finished
                        progressed = 20;

                    if (lastProgressed != progressed)
                    {
                        AwesomeConsole.Write("[");
                        for (int i = 0; i < 20; i++)
                        {
                            if (i < progressed)
                                AwesomeConsole.Write("=");
                            else if (i == progressed)
                                AwesomeConsole.Write(">");
                            else
                                AwesomeConsole.Write(" ");
                        }
                        AwesomeConsole.Write("]");
                        Console.CursorLeft = 0;

                        lastProgressed = progressed;
                    }
                }
                AwesomeConsole.WriteLine();

                AwesomeConsole.WriteLine("Done.", ConsoleColor.Green);
            }
        }

        private static uint FindPathMftId(Options opts)
        {
            string[] pathParts = opts.Source.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            string[] pathParents = pathParts.Skip(1).Take(pathParts.Length - 2).ToArray();

            if (pathParts.Length == 1)
            {
                // Chosen path is root
                return (uint)SpecialMFTFiles.RootDir;
            }

            using (RawDisk disk = new RawDisk(opts.Drive))
            {
                NTFSWrapper ntfs = new NTFSWrapper(new NTFSDiskProvider(disk), 4);

                // Navigate to parent directory
                NtfsDirectory parentDir = ntfs.GetRootDirectory();

                for (int i = 0; i < pathParents.Length; i++)
                {
                    parentDir = parentDir.ListDirectories(false).FirstOrDefault(s => s.Name.Equals(pathParents[i], StringComparison.InvariantCultureIgnoreCase));

                    if (parentDir == null)
                        return uint.MaxValue;
                }

                // Select the correct child
                IEnumerable<NtfsFileEntry> childs = parentDir.ListFiles(false);
                NtfsFileEntry child = childs.FirstOrDefault(s => s.Name.Equals(pathParts.Last(), StringComparison.InvariantCultureIgnoreCase));

                if (child == null)
                    return uint.MaxValue;

                // Return the childs id
                return child.MFTRecord.MFTNumber;
            }
        }

        private static void PrintError(string error)
        {
            AwesomeConsole.Write(error, ConsoleColor.Red);
        }
    }
}
