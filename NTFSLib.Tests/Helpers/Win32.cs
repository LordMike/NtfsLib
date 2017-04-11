using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using DeviceIOControlLib;
using DeviceIOControlLib.Wrapper;
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
            SafeFileHandle handle = CreateFile(fileName, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.OpenOrCreate, FileAttributes.Normal, IntPtr.Zero);

            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return handle;
        }

        public static FilesystemDeviceWrapper GetFileWrapper(string fileName)
        {
            return new FilesystemDeviceWrapper(CreateFile(fileName), true);
        }
    }
}
