using System;
using System.IO;

namespace NTFSLib.Tests.Helpers
{
    public class TempDir : IDisposable
    {
        public DirectoryInfo Directory { get; set; }

        public TempDir()
        {
            string tmpDir = Path.GetTempPath();

            while (Directory == null)
            {
                string dirName;
                using (TempFile tmpFile = new TempFile())
                {
                    dirName = tmpFile.File.Name;
                }

                DirectoryInfo dir = new DirectoryInfo(Path.Combine(tmpDir, dirName));

                if (!dir.Exists)
                {
                    // Use this
                    dir.Create();
                    Directory = dir;
                }
            }
        }

        public void Dispose()
        {
            Directory.Refresh();
            if (Directory.Exists)
                Directory.Delete(true);
        }
    }
}