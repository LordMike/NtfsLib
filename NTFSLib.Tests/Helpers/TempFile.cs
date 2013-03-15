using System;
using System.IO;

namespace NTFSLib.Tests.Helpers
{
    public class TempFile : IDisposable
    {
        public FileInfo File { get; set; }

        public TempFile()
        {
            File = new FileInfo(Path.GetTempFileName());
        }

        public void Dispose()
        {
            File.Refresh();
            if (File.Exists)
                File.Delete();
        }
    }
}
