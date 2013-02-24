using System;

namespace NTFSLib.Objects.Security
{
    [Flags]
    public enum ACEFlags : byte
    {
        ObjectInheritsACE = 0x01,
        ContainerInheritsACE = 0x02,
        DontPropagateInheritACE = 0x04,
        InheritOnlyACE = 0x08,
        AuditOnSuccess = 0x40,
        AuditOnFailure = 0x80
    }
}