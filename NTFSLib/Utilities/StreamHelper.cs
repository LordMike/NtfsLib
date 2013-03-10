using System;
using System.IO;
using System.Text;

namespace NTFSLib.Utilities
{
    public static class StreamHelper
    {
        public static string ReadString(this Stream stream, Encoding encoding, int bytes)
        {
            byte[] data = new byte[bytes];
            stream.Read(data, 0, data.Length);

            string res = encoding.GetString(data);
            return res.Substring(0, res.IndexOf('\0'));
        }

        public static uint ReadUint(this Stream stream)
        {
            byte[] data = new byte[4];
            stream.Read(data, 0, data.Length);

            return BitConverter.ToUInt32(data, 0);
        }

        public static int ReadInt(this Stream stream)
        {
            byte[] data = new byte[4];
            stream.Read(data, 0, data.Length);

            return BitConverter.ToInt32(data, 0);
        }

        public static ulong ReadUlong(this Stream stream)
        {
            byte[] data = new byte[8];
            stream.Read(data, 0, data.Length);

            return BitConverter.ToUInt64(data, 0);
        }

        public static void SkipBytes(this Stream stream, int count)
        {
            byte[] data = new byte[count];
            stream.Read(data, 0, data.Length);
        }
    }
}
