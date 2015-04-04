
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using NTFSLib.Helpers;
using NTFSLib.IO;
using NTFSLib.NTFS;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Security;
using NTFSLib.Objects.Specials;
using RawDiskLib;
using System.Linq;
using Attribute = NTFSLib.Objects.Attributes.Attribute;

namespace NtfsDetails
{
    static class Program
    {
        const string SingleIndent = "  ";

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

            // Work with the options
            if (opts.ActionType == ActionType.ShowFull || opts.ActionType == ActionType.ShowExtents)
            {
                uint mftId = 0;

                if (opts.PathType == PathType.File || opts.PathType == PathType.Directory)
                    mftId = FindPathMftId(opts);
                else if (opts.PathType == PathType.MftId)
                    mftId = opts.MftId;

                using (RawDisk disk = new RawDisk(opts.Drive))
                using (Stream stream = disk.CreateDiskStream())
                {
                    NTFSParser parser = new NTFSParser(stream);

                    if (parser.FileRecordCount < mftId)
                    {
                        PrintError("The given path or MftID didn't exist");
                        AwesomeConsole.WriteLine();

                        switch (opts.PathType)
                        {
                            case PathType.Directory:
                            case PathType.File:
                                PrintError("Specified " + opts.PathType + ": " + opts.PathArgument);
                                break;
                            case PathType.MftId:
                                PrintError("Specified type: MftId, id: " + opts.MftId);
                                break;
                        }

                        AwesomeConsole.WriteLine();

                        return;
                    }

                    if (opts.ActionType == ActionType.ShowFull)
                        PrintFull(parser, opts, mftId);
                    else if (opts.ActionType == ActionType.ShowExtents)
                        PrintExtents(parser, opts, mftId);
                }
            }
            else if (opts.ActionType == ActionType.Path)
            {
                using (RawDisk disk = new RawDisk(opts.Drive))
                {
                    PrintPaths(disk, opts);
                }
            }

            if (Debugger.IsAttached)
                Console.ReadLine();
        }

        private static void PrintPaths(RawDisk disk, Options options)
        {
            uint mftId = uint.MaxValue;

            if (options.PathType == PathType.MftId)
            {
                AwesomeConsole.WriteLine("Specified an Mft Id - skipping forward-only search", ConsoleColor.DarkGreen);

                mftId = options.MftId;
            }
            else if (options.PathType == PathType.File || options.PathType == PathType.Directory)
            {
                AwesomeConsole.WriteLine("Conducting forward-only path search", ConsoleColor.Green);

                string[] pathParts = options.PathArgument.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                string[] pathParents = pathParts.Skip(1).Take(pathParts.Length - 2).ToArray();

                NTFSWrapper ntfs = new NTFSWrapper(new NTFSDiskProvider(disk), 4);

                NtfsDirectory dir = ntfs.GetRootDirectory();
                PrintName(dir.Name);
                AwesomeConsole.Write(" ");
                PrintReference(dir.MFTRecord.FileReference);
                AwesomeConsole.WriteLine();

                string pathSoFar = options.Drive + ":\\";
                for (int i = 0; i < pathParents.Length; i++)
                {
                    AwesomeConsole.Write(pathSoFar, ConsoleColor.DarkYellow);

                    if (dir == null)
                    {
                        PrintName(pathParents[i]);
                        AwesomeConsole.Write(" ");
                        PrintError("(Unable to find parent)");
                        AwesomeConsole.WriteLine();
                    }
                    else
                    {
                        dir = dir.ListDirectories(false).FirstOrDefault(s => s.Name.Equals(pathParents[i], StringComparison.InvariantCultureIgnoreCase));

                        if (dir == null)
                        {
                            PrintName(pathParents[i]);
                            AwesomeConsole.Write(" ");
                            PrintError("(Unable to find this)");
                            AwesomeConsole.WriteLine();
                        }
                        else
                        {
                            PrintName(dir.Name);
                            AwesomeConsole.Write(" ");
                            PrintReference(dir.MFTRecord.FileReference);
                            AwesomeConsole.WriteLine();
                        }
                    }

                    pathSoFar = Path.Combine(pathSoFar, pathParents[i]+ "\\");
                }

                AwesomeConsole.Write(pathSoFar, ConsoleColor.DarkYellow);

                if (dir == null)
                {
                    PrintName(pathParts.Last());
                    AwesomeConsole.Write(" ");
                    PrintError("(Unable to find parent)");
                    AwesomeConsole.WriteLine();
                }
                else
                {
                    IEnumerable<NtfsFileEntry> childs = options.PathType == PathType.File ? (IEnumerable<NtfsFileEntry>)dir.ListFiles(false) : dir.ListDirectories(false);
                    NtfsFileEntry child = childs.FirstOrDefault(s => s.Name.Equals(pathParts.Last(), StringComparison.InvariantCultureIgnoreCase));

                    if (child == null)
                    {
                        PrintName(pathParts.Last());
                        AwesomeConsole.Write(" ");
                        PrintError("(Unable to find this)");
                        AwesomeConsole.WriteLine();
                    }
                    else
                    {
                        PrintName(child.Name);
                        AwesomeConsole.Write(" ");
                        PrintReference(child.MFTRecord.FileReference);
                        AwesomeConsole.WriteLine();

                        mftId = child.MFTRecord.MFTNumber;

                        AwesomeConsole.WriteLine("Search completed, found MftId: " + mftId);
                    }
                }
            }

            AwesomeConsole.WriteLine();

            {
                NTFSWrapper wrapper = new NTFSWrapper(new NTFSDiskProvider(disk), 4);

                if (wrapper.FileRecordCount < mftId)
                {
                    PrintError("Unable to locate the specified file, aborting.");
                    AwesomeConsole.WriteLine();
                    return;
                }

                AwesomeConsole.WriteLine("Conducting backwards-only path search", ConsoleColor.Green);

                Dictionary<uint, List<string>> paths = new Dictionary<uint, List<string>>();
                List<string> finalPaths = new List<string>();

                FileRecord baseRecord = wrapper.ReadMFTRecord(mftId);

                foreach (AttributeFileName fileName in baseRecord.Attributes.OfType<AttributeFileName>().Concat(baseRecord.ExternalAttributes.OfType<AttributeFileName>()))
                {
                    uint parentId = fileName.ParentDirectory.FileId;
                    if (paths.ContainsKey(parentId))
                        paths[parentId].Add(fileName.FileName);
                    else
                        paths[parentId] = new List<string> { fileName.FileName };
                }

                do
                {
                    Dictionary<uint, List<string>> newPaths = new Dictionary<uint, List<string>>();

                    foreach (KeyValuePair<uint, List<string>> keyValuePair in paths)
                    {
                        if (keyValuePair.Key == (uint)SpecialMFTFiles.RootDir)
                        {
                            finalPaths.AddRange(keyValuePair.Value.Select(s => Path.Combine(options.Drive + ":\\", s)));
                        }
                        else
                        {
                            FileRecord record = wrapper.ReadMFTRecord(keyValuePair.Key);

                            foreach (AttributeFileName fileName in record.Attributes.OfType<AttributeFileName>().Concat(record.ExternalAttributes.OfType<AttributeFileName>()))
                            {
                                uint parentId = fileName.ParentDirectory.FileId;
                                if (newPaths.ContainsKey(parentId))
                                    newPaths[parentId].AddRange(keyValuePair.Value.Select(s => Path.Combine(fileName.FileName, s)));
                                else
                                    newPaths[parentId] = new List<string>(keyValuePair.Value.Select(s => Path.Combine(fileName.FileName, s)));
                            }
                        }
                    }

                    paths = newPaths;
                } while (paths.Any());

                AwesomeConsole.WriteLine("Got " + finalPaths.Count + " paths");

                foreach (string finalPath in finalPaths)
                {
                    PrintName(finalPath);
                    AwesomeConsole.WriteLine();
                }
            }
        }

        private static void PrintExtents(NTFSParser parser, Options options, uint mftnumber)
        {
            // Read record
            List<FileRecord> records = new List<FileRecord>();

            parser.CurrentMftRecordNumber = mftnumber;
            records.Add(parser.ParseNextRecord());

            // Parse all references
            for (int i = 0; i < records.Count; i++)
            {
                List<AttributeList> lists = records[i].Attributes.OfType<AttributeList>().ToList();

                foreach (AttributeList attr in lists)
                {
                    if (attr.NonResidentFlag == ResidentFlag.NonResident)
                        parser.ParseNonResidentAttribute(attr);

                    foreach (AttributeListItem attributeListItem in attr.Items)
                    {
                        uint otherRecId = attributeListItem.BaseFile.FileId;

                        // Is new?
                        if (records.All(s => s.MFTNumber != otherRecId))
                        {
                            parser.CurrentMftRecordNumber = otherRecId;
                            records.Add(parser.ParseNextRecord());
                        }
                    }
                }
            }

            // Combine all streams
            List<Attribute> attributes = records.SelectMany(x => x.Attributes.Where(s => s.NonResidentFlag == ResidentFlag.NonResident)).ToList();

            var groups = attributes.GroupBy(s => new { s.Type, s.AttributeName }).OrderBy(s => s.Key.Type).ThenBy(s => s.Key.AttributeName).ToList();

            foreach (var @group in groups)
            {
                List<DataFragment> fragments = @group.SelectMany(s => s.NonResidentHeader.Fragments).OrderBy(s => s.StartingVCN).ToList();

                PrintType(group.Key.Type);
                AwesomeConsole.Write(" ");
                PrintName(group.Key.AttributeName, true, true);
                AwesomeConsole.Write(" ");
                AwesomeConsole.Write("{0:N0} fragments", fragments.Count);
                AwesomeConsole.WriteLine();

                foreach (DataFragment fragment in fragments)
                {
                    AwesomeConsole.Write(SingleIndent);
                    PrintRange(parser, options, fragment.LCN, fragment.Clusters);
                    AwesomeConsole.Write(" ");
                    PrintSize(parser, options, fragment.Clusters);
                    AwesomeConsole.WriteLine();
                }

                AwesomeConsole.WriteLine();
            }
        }

        private static void PrintFull(NTFSParser parser, Options options, uint mftnumber)
        {
            HashSet<FileReference> otherRecords = new HashSet<FileReference>();

            parser.CurrentMftRecordNumber = mftnumber;
            FileRecord record = parser.ParseNextRecord();

            using (AwesomeConsole.BeginSequentialWrite())
            {
                AwesomeConsole.Write("Record Id: ");
                PrintReference(record.FileReference);
                AwesomeConsole.Write(" Attributes: ");
                AwesomeConsole.Write(record.Attributes.Count.ToString("N0"));

                if (record.Flags.HasFlag(FileEntryFlags.FileInUse))
                    AwesomeConsole.Write(" (InUse)", ConsoleColor.Magenta);
                else
                    AwesomeConsole.Write(" (Not InUse)", ConsoleColor.Red);

                if (record.Flags.HasFlag(FileEntryFlags.Directory))
                    AwesomeConsole.Write(" (Directory)", ConsoleColor.Yellow);

                if (Enum.IsDefined(typeof(SpecialMFTFiles), record.MFTNumber))
                    AwesomeConsole.Write(" (Special MFT Record: " + (SpecialMFTFiles)record.MFTNumber + ")", ConsoleColor.White);
            }

            if (record.BaseFile.FileId != 0)
            {
                AwesomeConsole.Write(" (BaseFile: ", ConsoleColor.Cyan);
                PrintReference(record.BaseFile);
                AwesomeConsole.Write(")", ConsoleColor.Cyan);
            }

            AwesomeConsole.WriteLine();

            foreach (Attribute attribute in record.Attributes)
            {
                if (attribute.NonResidentFlag == ResidentFlag.NonResident &&
                    (attribute.Type == AttributeType.ATTRIBUTE_LIST || attribute.Type == AttributeType.INDEX_ALLOCATION || attribute.Type == AttributeType.SECURITY_DESCRIPTOR || attribute.Type == AttributeType.BITMAP))
                    parser.ParseNonResidentAttribute(attribute);

                PrettyPrintAttribute(parser, options, record, attribute, 1);

                if (attribute.Type == AttributeType.ATTRIBUTE_LIST)
                {
                    foreach (FileReference otherFile in ((AttributeList)attribute).Items.Select(s => s.BaseFile))
                        otherRecords.Add(otherFile);
                }

                AwesomeConsole.WriteLine();
            }

            // Don't go into infinite loops
            otherRecords.RemoveWhere(s => s.FileId == record.MFTNumber);

            foreach (FileReference fileReference in otherRecords)
            {
                AwesomeConsole.WriteLine();
                PrintFull(parser, options, fileReference.FileId);
            }
        }

        private static void PrettyPrintAttribute(NTFSParser parser, Options options, FileRecord record, Attribute attrib, int indentCount)
        {
            string indent = "";
            for (int i = 0; i < indentCount; i++)
                indent += SingleIndent;

            AwesomeConsole.Write(indent + attrib.Id + ": ");
            PrintType(attrib.Type);

            AwesomeConsole.Write(" ");
            PrintName(attrib.AttributeName, true, true);

            if (attrib.NonResidentFlag == ResidentFlag.NonResident)
                AwesomeConsole.Write(" (NonResident)", ConsoleColor.Red);

            AwesomeConsole.WriteLine();

            indent += SingleIndent;

            switch (attrib.Type)
            {
                case AttributeType.STANDARD_INFORMATION:
                    AttributeStandardInformation standardInformation = (AttributeStandardInformation)attrib;

                    AwesomeConsole.WriteLine(indent + "Creation Time: " + standardInformation.TimeCreated + " " + standardInformation.TimeCreated.Kind);
                    AwesomeConsole.WriteLine(indent + "Modified Time: " + standardInformation.TimeModified + " " + standardInformation.TimeModified.Kind);
                    AwesomeConsole.WriteLine(indent + "Accessed Time: " + standardInformation.TimeAccessed + " " + standardInformation.TimeAccessed.Kind);
                    AwesomeConsole.WriteLine(indent + "Mft Modified : " + standardInformation.TimeMftModified + " " + standardInformation.TimeMftModified.Kind);

                    break;
                case AttributeType.ATTRIBUTE_LIST:
                    AttributeList list = (AttributeList)attrib;

                    foreach (AttributeListItem listItem in list.Items)
                    {
                        AwesomeConsole.Write(indent + listItem.AttributeId + ": ");
                        PrintType(listItem.Type);
                        AwesomeConsole.Write(" ");

                        PrintName(listItem.Name, true, true);
                        AwesomeConsole.Write(" ");

                        if (record.FileReference == listItem.BaseFile)
                            AwesomeConsole.Write("(this record)", ConsoleColor.DarkGray);
                        else
                            PrintReference(listItem.BaseFile);

                        AwesomeConsole.WriteLine();
                    }

                    break;
                case AttributeType.FILE_NAME:
                    AttributeFileName fileName = (AttributeFileName)attrib;

                    using (AwesomeConsole.BeginSequentialWrite())
                    {
                        AwesomeConsole.Write(indent + "Parent dir: ");
                        AwesomeConsole.WriteLine(fileName.ParentDirectory, ConsoleColor.Cyan);
                    }

                    AwesomeConsole.WriteLine(indent + "Namespace: " + fileName.FilenameNamespace);

                    AwesomeConsole.Write(indent + "Flags: ");
                    PrintEnums(fileName.FileFlags);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.Write(indent + "Name: ");
                    PrintName(fileName.FileName, false, true);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.WriteLine(indent + "C Time: " + fileName.CTime + " " + fileName.CTime.Kind);
                    AwesomeConsole.WriteLine(indent + "M Time: " + fileName.MTime + " " + fileName.MTime.Kind);
                    AwesomeConsole.WriteLine(indent + "A Time: " + fileName.ATime + " " + fileName.ATime.Kind);
                    AwesomeConsole.WriteLine(indent + "R Time: " + fileName.RTime + " " + fileName.RTime.Kind);

                    break;
                case AttributeType.DATA:
                    AttributeData data = (AttributeData)attrib;

                    if (data.NonResidentFlag == ResidentFlag.Resident)
                    {
                        AwesomeConsole.WriteLine(indent + "Data length: {0:N0} Bytes", data.ResidentHeader.ContentLength);
                    }
                    else
                    {
                        AwesomeConsole.WriteLine(indent + "Data length: {0:N0} Bytes", data.NonResidentHeader.ContentSize);

                        AwesomeConsole.Write(indent + "VCN: ");
                        PrintRange(parser, options, data.NonResidentHeader.StartingVCN, data.NonResidentHeader.EndingVCN - data.NonResidentHeader.StartingVCN);
                        AwesomeConsole.WriteLine();

                        AwesomeConsole.WriteLine(indent + "Fragments: {0:N0}", data.NonResidentHeader.Fragments.Length);

                        AwesomeConsole.WriteLine(indent + SingleIndent + "LCN-range, cluster count, VCN-range", ConsoleColor.DarkGray);

                        foreach (DataFragment fragment in data.NonResidentHeader.Fragments)
                        {
                            AwesomeConsole.Write(indent + SingleIndent);
                            PrintRange(parser, options, fragment.LCN, fragment.Clusters);
                            AwesomeConsole.Write(SingleIndent);
                            PrintSize(parser, options, fragment.Clusters);
                            AwesomeConsole.Write(SingleIndent);
                            PrintRange(parser, options, fragment.StartingVCN, fragment.Clusters);

                            if (fragment.IsCompressed)
                                AwesomeConsole.Write(" (Compressed)");
                            if (fragment.IsSparseFragment)
                                AwesomeConsole.Write(" (Sparse)");

                            AwesomeConsole.WriteLine();
                        }
                    }

                    break;
                case AttributeType.OBJECT_ID:
                    AttributeObjectId objectId = (AttributeObjectId)attrib;

                    AwesomeConsole.Write(indent + "ObjectId    : ");
                    PrintGUID(objectId.ObjectId);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.Write(indent + "BithVolumeId: ");
                    PrintGUID(objectId.BithVolumeId);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.Write(indent + "BithObjectId: ");
                    PrintGUID(objectId.BithObjectId);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.Write(indent + "DomainId    : ");
                    PrintGUID(objectId.DomainId);
                    AwesomeConsole.WriteLine();

                    break;
                case AttributeType.SECURITY_DESCRIPTOR:
                    AttributeSecurityDescriptor securityDescriptor = (AttributeSecurityDescriptor)attrib;

                    AwesomeConsole.Write(indent + "SID: ");
                    PrintSID(securityDescriptor.UserSID);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.Write(indent + "GID: ");
                    PrintSID(securityDescriptor.GroupSID);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.Write(indent + "Flags: ");
                    PrintEnums(securityDescriptor.ControlFlags);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.WriteLine();

                    AwesomeConsole.WriteLine(indent + "SACL: " + (securityDescriptor.SACL == null ? 0 : securityDescriptor.SACL.ACECount));
                    if (securityDescriptor.SACL == null)
                        AwesomeConsole.WriteLine(indent + SingleIndent + "Not present", ConsoleColor.Red);
                    else
                        foreach (ACE ace in securityDescriptor.SACL.ACEs)
                        {
                            PrintACE(indent, ace);
                        }

                    AwesomeConsole.WriteLine(indent + "DACL: " + (securityDescriptor.DACL == null ? 0 : securityDescriptor.DACL.ACECount));
                    if (securityDescriptor.DACL == null)
                        AwesomeConsole.WriteLine(indent + SingleIndent + "Not present", ConsoleColor.Red);
                    else
                        foreach (ACE ace in securityDescriptor.DACL.ACEs)
                        {
                            PrintACE(indent, ace);
                        }

                    break;
                case AttributeType.VOLUME_NAME:
                    AttributeVolumeName volumeName = (AttributeVolumeName)attrib;

                    AwesomeConsole.Write(indent + "Name: ");
                    PrintName(volumeName.VolumeName);
                    AwesomeConsole.WriteLine();

                    break;
                case AttributeType.VOLUME_INFORMATION:
                    AttributeVolumeInformation volumeInformation = (AttributeVolumeInformation)attrib;

                    AwesomeConsole.WriteLine(indent + "Reserved: " + volumeInformation.Reserved);
                    AwesomeConsole.WriteLine(indent + "MajorVersion: " + volumeInformation.MajorVersion + "." + volumeInformation.MinorVersion);

                    AwesomeConsole.Write(indent + "VolumeInformationFlag: ");
                    PrintEnums(volumeInformation.VolumeInformationFlag);
                    AwesomeConsole.WriteLine();

                    break;
                case AttributeType.INDEX_ROOT:
                    AttributeIndexRoot indexRoot = (AttributeIndexRoot)attrib;

                    AwesomeConsole.WriteLine(indent + "IndexType: " + indexRoot.IndexType);
                    AwesomeConsole.WriteLine(indent + "CollationRule: " + indexRoot.CollationRule);
                    AwesomeConsole.WriteLine(indent + "IndexAllocationSize: " + indexRoot.IndexAllocationSize);
                    AwesomeConsole.WriteLine(indent + "ClustersPrIndexRecord: " + indexRoot.ClustersPrIndexRecord);
                    AwesomeConsole.WriteLine();

                    AwesomeConsole.WriteLine(indent + "SizeOfIndexTotal: " + indexRoot.SizeOfIndexTotal);
                    AwesomeConsole.WriteLine(indent + "IndexFlags: " + indexRoot.IndexFlags);
                    AwesomeConsole.WriteLine(indent + "Entries: " + indexRoot.Entries.Length);

                    foreach (IndexEntry entry in indexRoot.Entries)
                    {
                        AwesomeConsole.Write(indent + SingleIndent);
                        PrintReference(entry.FileRefence);

                        if (entry.ChildFileName != null)
                        {
                            AwesomeConsole.Write(" ");
                            PrintName(entry.ChildFileName.FileName, true);
                            AwesomeConsole.Write(" ");
                            PrintEnums(entry.ChildFileName.FileFlags);
                        }

                        AwesomeConsole.WriteLine();
                    }

                    break;
                case AttributeType.INDEX_ALLOCATION:
                    AttributeIndexAllocation indexAllocation = (AttributeIndexAllocation)attrib;

                    AwesomeConsole.WriteLine(indent + "Chunks: " + indexAllocation.Indexes.Length);

                    for (int i = 0; i < indexAllocation.Indexes.Length; i++)
                    {
                        IndexAllocationChunk chunk = indexAllocation.Indexes[i];
                        AwesomeConsole.WriteLine(indent + SingleIndent + string.Format("{0:N0}: {1:N0} of {2:N0} Bytes used", i, chunk.SizeOfIndexTotal, chunk.SizeOfIndexAllocated));
                    }

                    AwesomeConsole.WriteLine(indent + "Entries: " + indexAllocation.Entries.Length);

                    foreach (IndexEntry entry in indexAllocation.Entries)
                    {
                        AwesomeConsole.Write(indent + SingleIndent);
                        PrintReference(entry.FileRefence);

                        if (entry.ChildFileName != null)
                        {
                            AwesomeConsole.Write(" ");
                            PrintName(entry.ChildFileName.FileName, true);
                            AwesomeConsole.Write(" ");
                            PrintEnums(entry.ChildFileName.FileFlags);
                        }

                        AwesomeConsole.WriteLine();
                    }

                    break;
                case AttributeType.BITMAP:
                    AttributeBitmap bitmap = (AttributeBitmap)attrib;

                    AwesomeConsole.WriteLine(indent + "Bitfield Size: {0:N0} ({1:N0} bytes)", bitmap.Bitfield.Length, bitmap.Bitfield.Length / 8);

                    // Print out 4 lines of 64 bits
                    const int bitsPrLine = 64;

                    for (int line = 0; line < 4; line++)
                    {
                        if (bitmap.Bitfield.Length <= line * bitsPrLine)
                            break;

                        AwesomeConsole.Write(indent + "{0,-6}", (line * bitsPrLine) + ":");

                        for (int offset = line * bitsPrLine; offset < line * bitsPrLine + bitsPrLine; offset += 8)
                        {
                            if (bitmap.Bitfield.Length <= offset)
                                break;

                            for (int j = offset; j < offset + 8; j++)
                            {
                                if (bitmap.Bitfield.Length <= j)
                                    break;

                                AwesomeConsole.Write(bitmap.Bitfield[j] ? "1" : "0");
                            }

                            AwesomeConsole.Write(" ");
                        }

                        AwesomeConsole.WriteLine();
                    }

                    if (bitmap.Bitfield.Length > 256)
                    {
                        PrintError(indent + "Bitfield was longer than 256 bits, so the rest wasn't printed.");
                        AwesomeConsole.WriteLine();
                    }

                    break;
                case AttributeType.LOGGED_UTILITY_STREAM:
                    AttributeLoggedUtilityStream loggedUtilityStream = (AttributeLoggedUtilityStream)attrib;

                    AwesomeConsole.WriteLine(indent + "Data: {0:N0} Bytes", loggedUtilityStream.Data.Length);

                    break;
                default:
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    PrintError(attrib.Type + " not supported");
                    break;
            }
        }

        private static uint FindPathMftId(Options opts)
        {
            string[] pathParts = opts.PathArgument.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
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
                IEnumerable<NtfsFileEntry> childs = opts.PathType == PathType.File ? (IEnumerable<NtfsFileEntry>)parentDir.ListFiles(false) : parentDir.ListDirectories(false);
                NtfsFileEntry child = childs.FirstOrDefault(s => s.Name.Equals(pathParts.Last(), StringComparison.InvariantCultureIgnoreCase));

                if (child == null)
                    return uint.MaxValue;

                // Return the childs id
                return child.MFTRecord.MFTNumber;
            }
        }

        private static Enum[] GetEnumFlags(Enum value)
        {
            IEnumerable<Enum> valids = Enum.GetValues(value.GetType()).OfType<Enum>();

            return valids.Where(value.HasFlag).ToArray();
        }

        private static void PrintName(string name, bool enclose = false, bool writeUnamed = false)
        {
            using (AwesomeConsole.BeginSequentialWrite())
            {
                if (string.IsNullOrEmpty(name) && writeUnamed)
                {
                    if (enclose)
                        AwesomeConsole.Write("(unamed)", ConsoleColor.DarkGray);
                    else
                        AwesomeConsole.Write("unamed", ConsoleColor.DarkGray);

                    return;
                }

                // Write name
                if (enclose)
                    AwesomeConsole.Write("(", ConsoleColor.Yellow);

                AwesomeConsole.Write("\"", ConsoleColor.Yellow);
                AwesomeConsole.Write(name, ConsoleColor.Yellow);
                AwesomeConsole.Write("\"", ConsoleColor.Yellow);

                if (enclose)
                    AwesomeConsole.Write(")", ConsoleColor.Yellow);
            }
        }

        private static void PrintReference(FileReference reference)
        {
            AwesomeConsole.Write(reference, ConsoleColor.Cyan);
        }

        private static void PrintType(AttributeType type)
        {
            AwesomeConsole.Write(type, ConsoleColor.Green);
        }

        private static void PrintEnums(Enum value)
        {
            Enum[] enumFlags = GetEnumFlags(value);
            if (enumFlags.Any())
                AwesomeConsole.Write("(" + string.Join(", ", enumFlags.Select(s => s.ToString())) + ")", ConsoleColor.Magenta);
        }

        private static void PrintSID(SecurityIdentifier sid)
        {
            AwesomeConsole.Write(sid, ConsoleColor.DarkCyan);

            foreach (WellKnownSidType wellKnownSidType in Enum.GetValues(typeof(WellKnownSidType)).OfType<WellKnownSidType>())
                if (sid.IsWellKnown(wellKnownSidType))
                {
                    AwesomeConsole.Write(" (" + wellKnownSidType + ")", ConsoleColor.Yellow);
                    break;
                }
        }

        private static void PrintGUID(Guid guid)
        {
            if (guid == Guid.Empty)
                AwesomeConsole.Write("unset", ConsoleColor.DarkGray);
            else
                AwesomeConsole.Write(guid);
        }

        private static void PrintError(string error)
        {
            AwesomeConsole.Write(error, ConsoleColor.Red);
        }

        private static void PrintACE(string indent, ACE ace)
        {
            AwesomeConsole.Write(indent + SingleIndent + "SID: ");
            PrintSID(ace.SID);
            AwesomeConsole.WriteLine();

            AwesomeConsole.Write(indent + SingleIndent + SingleIndent + "Access: ");
            PrintEnums(ace.AccessMask);
            AwesomeConsole.WriteLine();

            AwesomeConsole.WriteLine(indent + SingleIndent + SingleIndent + "Type  : " + ace.Type);

            AwesomeConsole.Write(indent + SingleIndent + SingleIndent + "Flags : ");
            PrintEnums(ace.Flags);
            AwesomeConsole.WriteLine();
        }

        private static void PrintSize(INTFSInfo ntfs, Options opts, long clusters)
        {
            switch (opts.SizeUnit)
            {
                case SizeUnit.Bytes:
                    AwesomeConsole.Write("({0:N0} bytes)", ConsoleColor.DarkGreen, clusters * ntfs.BytesPrCluster);
                    break;
                case SizeUnit.Sectors:
                    AwesomeConsole.Write("({0:N0} sectors)", ConsoleColor.DarkGreen, clusters * ntfs.SectorsPrCluster);
                    break;
                default:
                    AwesomeConsole.Write("({0:N0} clusters)", ConsoleColor.DarkGreen, clusters);
                    break;
            }
        }

        private static void PrintRange(INTFSInfo ntfs, Options opts, long start, long count)
        {
            switch (opts.SizeUnit)
            {
                case SizeUnit.Bytes:
                    AwesomeConsole.Write("{0:N0} -> {1:N0}", start * ntfs.BytesPrCluster, (start + count) * ntfs.BytesPrCluster);
                    break;
                case SizeUnit.Sectors:
                    AwesomeConsole.Write("{0:N0} -> {1:N0}", start * ntfs.SectorsPrCluster, (start + count) * ntfs.SectorsPrCluster);
                    break;
                default:
                    AwesomeConsole.Write("{0:N0} -> {1:N0}", start, start + count);
                    break;
            }
        }
    }
}
