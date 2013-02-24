namespace NTFSLib.Objects.Enums
{
    public enum AttributeFlags : ushort
    {
        Compressed = 0x0001,
        Encrypted = 0x4000,
        Sparse = 0x8000
    }
}