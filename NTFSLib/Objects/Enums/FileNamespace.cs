namespace NTFSLib.Objects.Enums
{
    // ReSharper disable InconsistentNaming
    public enum FileNamespace : byte
    {
        POSIX = 0,
        Win32 = 1,
        DOS = 2,
        Win32AndDOS = 3
    }
    // ReSharper restore InconsistentNaming
}