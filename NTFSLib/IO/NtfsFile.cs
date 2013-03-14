using System.Diagnostics;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;

namespace NTFSLib.IO
{
    public class NtfsFile : NtfsFileEntry
    {
        internal NtfsFile(NTFS ntfs, FileRecord record, AttributeFileName fileName)
            : base(ntfs, record, fileName)
        {
            Debug.Assert(!record.Flags.HasFlag(FileEntryFlags.Directory));
        }

        public override string ToString()
        {
            return FileName.FileName;
        }
    }
}