namespace NTFSLib.Objects.Specials.Files
{
    public enum AttrDefCollationRule
    {
        Binary = 0x00,
        Filename = 0x01,
        UnicodeString = 0x02,
        UnsignedLong = 0x10,
        SID = 0x11,
        SecurityHash = 0x12,
        MultipleUnsignedLongs = 0x13
    }
}