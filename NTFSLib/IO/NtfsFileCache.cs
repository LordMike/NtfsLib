using System;
using System.Collections.Generic;

namespace NTFSLib.IO
{
    internal class NtfsFileCache
    {
        private Dictionary<ulong, WeakReference> _entries;

        internal NtfsFileCache()
        {
            _entries = new Dictionary<ulong, WeakReference>();
        }

        private ulong CreateKey(uint id, int filenameHashcode)
        {
            ulong key = (ulong)id << 32;

            if (filenameHashcode > 0)
                key |= (ulong)filenameHashcode;
            else
            {
                ulong tmp = (ulong)(-filenameHashcode);
                tmp += (uint)1 << 31;     // the 1-bit that's normally the sign bit
                key |= tmp;
            }

            return key;
        }

        public NtfsFileEntry Get(uint id, int filenameHashcode)
        {
            // Make combined key
            ulong key = CreateKey(id, filenameHashcode);

            // Fetch
            WeakReference tmp;
            _entries.TryGetValue(key, out tmp);

            if (tmp == null || !tmp.IsAlive)
                return null;

            return tmp.Target as NtfsFileEntry;
        }

        public void Set(uint id, ushort attributeId, NtfsFileEntry entry)
        {
            // Make combined key
            ulong key = CreateKey(id, attributeId);

            // Set
            _entries[key] = new WeakReference(entry);
        }
    }
}