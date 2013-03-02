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
            for (uint i = 20000; i < ntfs.FileRecordCount; i++)
            {
                FileRecord record = ntfs.ReadMFTRecord(i);
                ntfs.ParseAttributeLists(record);

                if (record.BaseFile.RawId != 0)
                    continue;

                var attributeData = record.Attributes.OfType<AttributeData>().Where(s => (s.NonResidentFlag == ResidentFlag.Resident && s.ResidentHeader.AttributeName == "") || (s.NonResidentFlag == ResidentFlag.NonResident && s.NonResidentHeader.AttributeName == "")).ToList();

                if (attributeData.Any(s => s.NonResidentFlag == ResidentFlag.Resident))
                    continue;

                if (attributeData.SelectMany(s => s.DataFragments).Count() <= 1)
                    continue;

                if (attributeData.First().NonResidentHeader.ContentSize > 512000000)
                    continue;

                var stream = ntfs.OpenFileRecord(record);

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                File.WriteAllBytes(@"E:\Mike\Dropbox\NTFSLib\out.bin", data);
            }


            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
