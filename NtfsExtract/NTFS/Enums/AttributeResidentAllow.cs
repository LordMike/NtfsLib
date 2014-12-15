using System;

namespace NtfsExtract.NTFS.Enums
{
    [Flags]
    public enum AttributeResidentAllow
    {
        Resident = 1,
        NonResident = 2
    }
}