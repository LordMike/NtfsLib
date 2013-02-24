namespace NTFSLib.Objects.Security
{
    public enum ACEType : byte
    {
        AccessAllowed = 0x00,
        AccessDenied = 0x01,
        SystemAudit = 0x02
    }
}