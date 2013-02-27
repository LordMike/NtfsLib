using System;

namespace NTFSLib
{
    public static class Utils
    {
        private static long _maxFileTime = DateTime.MaxValue.ToFileTimeUtc();

        public static DateTime FromWinFileTime(byte[] data, int offset)
        {
            long fileTime = BitConverter.ToInt64(data, offset);

            if (fileTime >= _maxFileTime)
                return DateTime.MaxValue;

            return DateTime.FromFileTimeUtc(fileTime);
        }
    }
}
