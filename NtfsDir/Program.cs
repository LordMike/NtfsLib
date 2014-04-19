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

namespace NtfsDir
{
    public static class Program
    {
        /* TODO: 
         * List streams ($DATA, $BITMAP ..)
         * List alternate namings (Win32, DOS, POSIX)
         * Toggle FileID's
         * List matching attributes 
         * Recursive
         * Deleted records
         */

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

            using (RawDisk disk = new RawDisk(opts.Drive))
            {
                NTFSWrapper wrapper = new NTFSWrapper(new NTFSDiskProvider(disk), 1);

                NtfsDirectory dir = null;

                if (opts.PathType == PathType.Directory)
                    dir = FindDir(wrapper, opts);

                if (dir == null)
                {
                    PrintError("The given path didn't exist");
                    AwesomeConsole.WriteLine();

                    switch (opts.PathType)
                    {
                        case PathType.Directory:
                            PrintError("Specified " + opts.PathType + ": " + opts.PathArgument);
                            break;
                    }

                    AwesomeConsole.WriteLine();
                    return;
                }

                // Display
                DisplayDetails(wrapper, opts, dir);
            }

            if (Debugger.IsAttached)
                Console.ReadLine();
        }

        private static void DisplayDetails(NTFSWrapper wrapper, Options opts, NtfsDirectory dir)
        {
            Console.WriteLine("Listing details for " + dir.Name);

            IEnumerable<NtfsFileEntry> subDirs = dir.ListDirectories(!opts.ShowAllNames);
            IEnumerable<NtfsFileEntry> subFiles = dir.ListFiles(!opts.ShowAllNames);

            foreach (NtfsFileEntry entry in subDirs.Concat(subFiles))
            {
                if (opts.ShowAllStreams)
                {
                    // Stream display
                    var streams = entry.MFTRecord.Attributes.Concat(entry.MFTRecord.ExternalAttributes).GroupBy(s => new { s.AttributeName, s.Type });

                    foreach (var stream in streams)
                    {
                        


                        AwesomeConsole.WriteLine();
                    }
                }
                else
                {
                    // Simple file display
                    AwesomeConsole.Write(entry.TimeModified.ToString("yyyy-MM-dd HH:mm"));
                    AwesomeConsole.Write(" ");

                    if (opts.ShowFileIds)
                    {
                        AwesomeConsole.Write(entry.MFTRecord.FileReference);
                        AwesomeConsole.Write(" ");
                    }

                    if (entry is NtfsDirectory)
                    {
                        AwesomeConsole.Write("<DIR>");
                    }
                    else
                    {
                        AttributeData dataAttrib = entry.MFTRecord.Attributes.OfType<AttributeData>().FirstOrDefault(s => s.NameLength == 0);

                        long fileSize = -1;
                        if (dataAttrib != null && dataAttrib.NonResidentFlag == ResidentFlag.Resident)
                            fileSize = dataAttrib.ResidentHeader.ContentLength;
                        else if (dataAttrib != null && dataAttrib.NonResidentFlag == ResidentFlag.NonResident)
                            fileSize = (long)dataAttrib.NonResidentHeader.ContentSize;

                        AwesomeConsole.Write(fileSize.ToString("N0"));
                    }

                    AwesomeConsole.Write(" ");
                    AwesomeConsole.Write(entry.Name);
                    AwesomeConsole.WriteLine();
                }
            }

            // Volume in drive C has no label.
            // Volume Serial Number is 50C3-B38B

            // Directory of C:\

            //23-08-2012  12:51             1.024 .rnd
            //12-05-2013  13:04    <DIR>          AMD
            //03-03-2013  09:51    <SYMLINKD>     Cygwin [C:\Program Files (x86)\Cygwin]
            //14-11-2012  23:24    <DIR>          Intel
            //14-07-2009  05:20    <DIR>          PerfLogs
            //20-05-2013  18:14    <DIR>          Program Files
            //20-05-2013  18:20    <DIR>          Program Files (x86)
            //12-05-2013  13:08    <DIR>          ProgramData
            //11-05-2013  14:49    <DIR>          Python27
            //18-01-2013  02:13    <DIR>          Temp
            //19-05-2013  18:21       378.273.792 test.bin
            //24-02-2013  02:32    <DIR>          Users
            //21-05-2013  13:37    <DIR>          Windows
            //               2 File(s)    378.274.816 bytes
            //              11 Dir(s)  21.434.728.448 bytes free

        }

        private static NtfsDirectory FindDir(NTFSWrapper wrapper, Options opts)
        {
            string[] pathParts = opts.PathArgument.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

            if (pathParts.Length == 0)
            {
                // Chosen path is root
                return wrapper.GetRootDirectory();
            }

            // Navigate to directory
            NtfsDirectory dir = wrapper.GetRootDirectory();

            for (int i = 0; i < pathParts.Length; i++)
            {
                dir = dir.ListDirectories(false).FirstOrDefault(s => s.Name.Equals(pathParts[i], StringComparison.InvariantCultureIgnoreCase));

                if (dir == null)
                    return null;
            }

            // Return the last directory
            return dir;
        }

        private static void PrintError(string error)
        {
            AwesomeConsole.Write(error, ConsoleColor.Red);
        }
    }
}
