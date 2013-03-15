using System;
using System.IO;
using System.Runtime.InteropServices;
using DeviceIOControlLib;
using Microsoft.Win32.SafeHandles;
using FileAttributes = System.IO.FileAttributes;

namespace NTFSLib.Tests.Helpers
{
    public static class Win32
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
           string lpFileName,
           [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
           [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
           IntPtr lpSecurityAttributes,
           [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
           [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
           IntPtr hTemplateFile);

        public static SafeFileHandle CreateFile(string fileName)
        {
            return CreateFile(fileName, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
        }

        public static DeviceIOControlWrapper GetFileWrapper(string fileName)
        {
            return new DeviceIOControlWrapper(CreateFile(fileName), true);
        }
    }
}
