using System;

namespace NTFSLib.Tests.Helpers
{
    public static class ArrayUtils
    {
        public static bool SequanceEqualIn<T>(this T[] thisArray, T[] checkIn) where T : IEquatable<T>
        {
            if (checkIn.Length < thisArray.Length)
                return false;

            for (int i = 0; i < thisArray.Length; i++)
            {
                if (!Equals(thisArray[i], checkIn[i]))
                    return false;
            }

            return true;
        }
    }
}
