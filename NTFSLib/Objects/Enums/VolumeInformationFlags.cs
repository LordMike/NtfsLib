namespace NTFSLib.Objects.Enums
{
    // ReSharper disable InconsistentNaming
    public enum VolumeInformationFlags : ushort
    {
        Dirty = 0x0001,
        ResizeLogFile = 0x0002,
        UpgradeOnMount = 0x0004,
        MountedOnNT4 = 0x0008,
        DeleteUSNUnderway = 0x0010,
        RepairObjectIds = 0x0020,
        ModifiedByChkdsk = 0x8000
    }
    // ReSharper restore InconsistentNaming
}