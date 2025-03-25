using System;
using System.IO;
using System.Runtime.InteropServices;

namespace _1BRC.Framework.Console.Generate {
	internal class NativeMethods {
		internal const int FileAttributeNormal = 0x00000080;

		internal const int FileFlagSequentialScan = 0x08000000;
		internal const uint FileFlagWriteThrough = 0x80000000;
		internal const uint FileAttributeFastReadFile = FileFlagSequentialScan | FileFlagWriteThrough;

		internal const int InvalidHandleValue = -1;
		internal const int GenericRead = -2147483648;
		internal const int GenericWrite = 1073741824;

		[DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
		internal static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

		[DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
		internal static extern unsafe void CopyMemory(byte* dest, byte* src, uint count);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern unsafe int WriteFile(
			IntPtr handle,
			byte* lpBuffer,
			uint nNumberOfBytesToWrite,
			out uint lpNumberOfBytesWritten,
			IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern unsafe int ReadFile(
			IntPtr hFile,
			byte* lpBuffer,
			int nNumberOfBytesToRead,
			out int lpNumberOfBytesRead,
			IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr CreateFile(
			string lpFileName,
			int dwDesiredAccess,
			FileShare dwShareMode,
			IntPtr securityAttrs,
			FileMode dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr hObject);
	}
}