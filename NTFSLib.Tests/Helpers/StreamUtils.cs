using System.IO;
using System.Linq;

namespace NTFSLib.Tests.Helpers
{
    public static class StreamUtils
    {
        public static bool CompareStreams(Stream a, Stream b)
        {
            if (a.Length != b.Length)
                return false;

            if (a.Length <= 1024)
                return false;

            // Do multiple comparisons
            for (int i = 0; i < a.Length / 2; i += 1000)        // 1000 is an odd number - should weed out boundary issues
            {
                a.Seek(i, SeekOrigin.Begin);
                b.Seek(i, SeekOrigin.Begin);

                if (!CompareStreamsDirectly(a, b, (int) (a.Length - i * 2)))
                    return false;
            }

            return true;
        }

        private static bool CompareStreamsDirectly(Stream a, Stream b, int length)
        {
            if (a.Length != b.Length)
                return false;

            byte[] dataA = new byte[length];
            byte[] dataB = new byte[length];

            int readA = a.Read(dataA, 0, dataA.Length);
            int readB = b.Read(dataB, 0, dataB.Length);

            if (readA != dataA.Length)
                return false;

            if (readB != dataB.Length)
                return false;

            return dataA.SequenceEqual(dataB);
        }
    }
}