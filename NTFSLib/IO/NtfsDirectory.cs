using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NTFSLib.Objects;
using NTFSLib.Objects.Attributes;
using NTFSLib.Objects.Enums;
using NTFSLib.Objects.Specials;

namespace NTFSLib.IO
{
    public class NtfsDirectory : NtfsFileEntry
    {
        private const string DirlistAttribName = "$I30";

        private AttributeIndexRoot _indexRoot;
        private AttributeIndexAllocation[] _indexes;

        internal NtfsDirectory(NTFS ntfs, FileRecord record, AttributeFileName fileName)
            : base(ntfs, record, fileName)
        {
            Debug.Assert(record.Flags.HasFlag(FileEntryFlags.Directory));

            PrepRecord();
        }

        private void PrepRecord()
        {
            // Ensure we have all INDEX attributes at hand
            bool parseLists = false;
            foreach (AttributeList list in MFTRecord.Attributes.OfType<AttributeList>())
            {
                foreach (AttributeListItem item in list.Items)
                {
                    if (item.BaseFile != MFTRecord.FileReference &&
                        (item.Type == AttributeType.INDEX_ROOT || item.Type == AttributeType.INDEX_ALLOCATION))
                    {
                        // We need to parse lists
                        parseLists = true;
                    }
                }
            }

            if (parseLists)
                Ntfs.ParseAttributeLists(MFTRecord);

            // Get root
            _indexRoot = MFTRecord.Attributes.OfType<AttributeIndexRoot>().Single(s => s.AttributeName == DirlistAttribName);

            // Get allocations
            _indexes = MFTRecord.Attributes.OfType<AttributeIndexAllocation>().Where(s => s.AttributeName == DirlistAttribName).ToArray();

            foreach (AttributeIndexAllocation index in _indexes)
            {
                Ntfs.ParseNonResidentAttribute(index);
            }

            // Get bitmap of allocations
            // var bitmap = MFTRecord.Attributes.OfType<AttributeBitmap>().Single(s => s.AttributeName == DirlistAttribName);
        }

        public IEnumerable<NtfsDirectory> ListDirectories(bool uniqueOnly = true)
        {
            return ListChilds(uniqueOnly).OfType<NtfsDirectory>();
        }

        public IEnumerable<NtfsFile> ListFiles(bool uniqueOnly = true)
        {
            return ListChilds(uniqueOnly).OfType<NtfsFile>();
        }

        public IEnumerable<NtfsFileEntry> ListChilds(bool uniqueOnly)
        {
            if (uniqueOnly)
            {
                FileNamespaceComparer comparer = new FileNamespaceComparer();
                Dictionary<uint, NtfsFileEntry> entries = new Dictionary<uint, NtfsFileEntry>();

                foreach (IndexEntry entry in _indexRoot.Entries)
                {
                    if (entries.ContainsKey((uint)entry.FileRefence.FileId))
                    {
                        // Is this better?
                        int comp = comparer.Compare(entry.ChildFileName.FilenameNamespace, entries[(uint)entry.FileRefence.FileId].FileName.FilenameNamespace);

                        if (comp == 1)
                        {
                            // New entry is better
                            entries[(uint)entry.FileRefence.FileId] = CreateEntry((uint)entry.FileRefence.FileId, entry.ChildFileName);
                        }
                    }
                    else
                        entries[(uint)entry.FileRefence.FileId] = CreateEntry((uint)entry.FileRefence.FileId, entry.ChildFileName);
                }

                if (_indexRoot.IndexFlags.HasFlag(MFTIndexRootFlags.LargeIndex))
                {
                    foreach (AttributeIndexAllocation index in _indexes)
                    {
                        foreach (IndexEntry entry in index.Entries)
                        {
                            if (entries.ContainsKey((uint)entry.FileRefence.FileId))
                            {
                                // Is this better?
                                int comp = comparer.Compare(entry.ChildFileName.FilenameNamespace, entries[(uint)entry.FileRefence.FileId].FileName.FilenameNamespace);

                                if (comp == 1)
                                {
                                    // New entry is better
                                    entries[(uint)entry.FileRefence.FileId] = CreateEntry((uint)entry.FileRefence.FileId, entry.ChildFileName);
                                }
                            }
                            else
                                entries[(uint)entry.FileRefence.FileId] = CreateEntry((uint)entry.FileRefence.FileId, entry.ChildFileName);
                        }
                    }
                }

                foreach (NtfsFileEntry value in entries.Values)
                {
                    yield return value;
                }
            }
            else
            {
                foreach (IndexEntry entry in _indexRoot.Entries)
                {
                    yield return CreateEntry((uint)entry.FileRefence.FileId, entry.ChildFileName);
                }

                if (_indexRoot.IndexFlags.HasFlag(MFTIndexRootFlags.LargeIndex))
                {
                    foreach (AttributeIndexAllocation index in _indexes)
                    {
                        foreach (IndexEntry entry in index.Entries)
                        {
                            yield return CreateEntry((uint)entry.FileRefence.FileId, entry.ChildFileName);
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return FileName.FileName + "\\";
        }
    }
}
