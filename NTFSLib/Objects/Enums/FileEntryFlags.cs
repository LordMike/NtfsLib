using System;

namespace NTFSLib.Objects.Enums
{
    [Flags]
    public enum FileEntryFlags : ushort
    {
        FileInUse = 1,
        Directory = 2
    }
}