using System;
using System.Diagnostics;
using System.Text;

namespace NTFSLib.Objects.Specials
{
    public class BootSector
    {
        public byte[] JmpInstruction { get; set; }
        public string OEMCode { get; set; }
        public ushort BytesPrSector { get; set; }
        public byte SectorsPrCluster { get; set; }
        public ushort ReservedSectors { get; set; }
        public byte MediaDescriptor { get; set; }
        public ushort SectorsPrTrack { get; set; }
        public ushort NumberOfHeads { get; set; }
        public uint HiddenSectors { get; set; }
        public uint Usually80008000 { get; set; }
        public ulong TotalSectors { get; set; }
        public ulong MFTCluster { get; set; }
        public ulong MFTMirrCluster { get; set; }
        public uint MFTRecordSizeClusters { get; set; }
        public uint MFTIndexSizeClusters { get; set; }
        public ulong SerialNumber { get; set; }
        public uint Checksum { get; set; }
        public byte[] BootstrapCode { get; set; }
        public byte[] Signature { get; set; }

        public static BootSector ParseData(byte[] data, int maxLength, int offset)
        {
            Debug.Assert(data.Length - offset >= 512);
            Debug.Assert(0 <= offset && offset <= data.Length);

            //0x0000	3	Jump to the boot loader routine
            //0x0003	8	System Id: "NTFS    "
            //0x000B	2	Bytes per sector
            //0x000D	1	Sectors per cluster
            //0x000E	7	Unused
            //0x0015	1	Media descriptor (a)
            //0x0016	2	Unused
            //0x0018	2	Sectors per track
            //0x001A	2	Number of heads
            //0x001C	8	Unused
            //0x0024	4	Usually 80 00 80 00 (b)
            //0x0028	8	Number of sectors in the volume
            //0x0030	8	LCN of VCN 0 of the $MFT
            //0x0038	8	LCN of VCN 0 of the $MFTMirr
            //0x0040	4	Clusters per MFT Record (c)
            //0x0044	4	Clusters per Index Record (c)
            //0x0048	8	Volume serial number

            BootSector res = new BootSector();

            res.JmpInstruction = new byte[3];
            Array.Copy(data, offset, res.JmpInstruction, 0, 3);

            res.OEMCode = Encoding.ASCII.GetString(data, offset + 3, 8);
            res.BytesPrSector = BitConverter.ToUInt16(data, offset + 11);
            res.SectorsPrCluster = data[offset + 13];
            res.ReservedSectors = BitConverter.ToUInt16(data, offset + 14);
            res.MediaDescriptor = data[offset + 21];
            res.SectorsPrTrack = BitConverter.ToUInt16(data, offset + 24);
            res.NumberOfHeads = BitConverter.ToUInt16(data, offset + 26);
            res.HiddenSectors = BitConverter.ToUInt32(data, offset + 28);
            res.Usually80008000 = BitConverter.ToUInt32(data, offset + 36);
            res.TotalSectors = BitConverter.ToUInt64(data, offset + 40);
            res.MFTCluster = BitConverter.ToUInt64(data, offset + 48);
            res.MFTMirrCluster = BitConverter.ToUInt64(data, offset + 56);
            res.MFTRecordSizeClusters = BitConverter.ToUInt32(data, offset + 64);
            res.MFTIndexSizeClusters = BitConverter.ToUInt32(data, offset + 68);
            res.SerialNumber = BitConverter.ToUInt64(data, offset + 72);
            res.Checksum = BitConverter.ToUInt32(data, offset + 80);

            res.BootstrapCode = new byte[426];
            Array.Copy(data, offset + 84, res.BootstrapCode, 0, 426);

            res.Signature = new byte[2];
            Array.Copy(data, offset + 510, res.Signature, 0, 2);

            return res;
        }
    }
}
