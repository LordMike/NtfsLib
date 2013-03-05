using System;
using System.IO;
using NTFSLib;
using NTFSLib.Helpers;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using RawDiskLib;
using System.Linq;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            RawDisk disk = new RawDisk('E');
            NTFSDiskProvider provider = new NTFSDiskProvider(disk);

            NTFS ntfs = new NTFS(provider);
            ntfs.InitializeCommon();

            Console.WriteLine("Read NTFS. Version: " + ntfs.NTFSVersion);

            // Read fragmented file
            for (uint i = 28; i < ntfs.FileRecordCount; i++)
            {
                FileRecord record = ntfs.ReadMFTRecord(i);
                ntfs.ParseAttributeLists(record);

                Console.WriteLine("Read " + i);

                if (record.BaseFile.RawId != 0)
                    continue;

                var attributeData = record.Attributes.OfType<AttributeData>().Where(s => (s.NonResidentFlag == ResidentFlag.Resident && s.ResidentHeader.AttributeName == "") || (s.NonResidentFlag == ResidentFlag.NonResident && s.NonResidentHeader.AttributeName == "")).ToList();

                if (attributeData.Any(s => s.NonResidentFlag == ResidentFlag.Resident))
                    continue;

                if (attributeData.SelectMany(s => s.DataFragments).Count() <= 1)
                    continue;

                if (attributeData.First().NonResidentHeader.ContentSize > 512000000)
                    continue;

                Console.WriteLine("Is a candidate for copying: " + i);

                //Stream stream = ntfs.OpenFileRecord(record);

                //using (FileStream fsOut = File.OpenWrite(@"E:\Mike\Dropbox\NTFSLib\out.bin"))
                //{
                //    fsOut.SetLength((long) attributeData.First().NonResidentHeader.ContentSize);

                //    stream.CopyTo(fsOut);
                //}
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
