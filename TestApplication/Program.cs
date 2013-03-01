using System;
using NTFSLib;
using NTFSLib.Helpers;
using RawDiskLib;

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

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
