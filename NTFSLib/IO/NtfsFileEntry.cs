using System.Diagnostics;
using System.IO;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using System.Linq;

namespace NTFSLib.IO
{
    public abstract class NtfsFileEntry
    {
        protected NTFS Ntfs;
        public FileRecord MFTRecord { get; private set; }

        internal AttributeFileName FileName;

        public string Name
        {
            get { return FileName.FileName; }
        }

        public NtfsDirectory Parent
        {
            get
            {
                return CreateEntry((uint)FileName.ParentDirectory.FileId) as NtfsDirectory;
            }
        }

        protected NtfsFileEntry(NTFS ntfs, FileRecord record, AttributeFileName fileName)
        {
            Ntfs = ntfs;
            MFTRecord = record;

            FileName = fileName;
        }

        internal NtfsFileEntry CreateEntry(uint fileId, AttributeFileName fileName = null)
        {
            return CreateEntry(Ntfs, fileId, fileName);
        }

        internal static NtfsFileEntry CreateEntry(NTFS ntfs, uint fileId, AttributeFileName fileName = null)
        {
            if (fileName == null)
            {
                // Dig up a preferred name
                FileRecord tmpRecord = ntfs.ReadMFTRecord(fileId);
                fileName = Utils.GetPreferredDisplayName(tmpRecord);
            }

            NtfsFileEntry entry = ntfs.FileCache.Get(fileId, fileName.FileName.GetHashCode());

            if (entry != null)
            {
                Debug.WriteLine("Got from cache: " + fileId + ":" + fileName.Id);
                return entry;
            }

            // Create it
            FileRecord record = ntfs.ReadMFTRecord(fileId);

            if (record.Flags.HasFlag(FileEntryFlags.Directory))
                entry = new NtfsDirectory(ntfs, record, fileName);
            else
                entry = new NtfsFile(ntfs, record, fileName);

            ntfs.FileCache.Set(fileId, fileName.Id, entry);

            return entry;
        }

        public string[] GetStreamList()
        {
            return MFTRecord.Attributes.OfType<AttributeData>().Select(s => s.AttributeName).ToArray();
        }

        public Stream OpenRead(string dataStream = "")
        {
            return Ntfs.OpenFileRecord(MFTRecord, dataStream);
        }
    }
}