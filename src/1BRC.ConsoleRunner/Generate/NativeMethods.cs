using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace _1BRC.Framework.Console.Generate {
	internal class NativeMethods {
		public const int FileReadData = 0x0001; // file & pipe
		public const int FileListDirectory = 0x0001; // directory
		public const int FileWriteData = 0x0002; // file & pipe
		public const int FileAddFile = 0x0002; // directory
		public const int FileAppendData = 0x0004; // file
		public const int FileAddSubdirectory = 0x0004; // directory
		public const int FileCreatePipeInstance = 0x0004; // named pipe
		public const int FileReadEa = 0x0008; // file & directory
		public const int FileWriteEa = 0x0010; // file & directory
		public const int FileExecute = 0x0020; // file
		public const int FileTraverse = 0x0020; // directory
		public const int FileDeleteChild = 0x0040; // directory
		public const int FileReadAttributes = 0x0080; // all
		public const int FileWriteAttributes = 0x0100; // all

		public const long StandardRightsRequired = 0x000F0000L;
		public const long ReadControl = 0x00020000L;
		public const long Synchronize = 0x00100000L;
		public const long StandardRightsRead = ReadControl;
		public const long StandardRightsWrite = ReadControl;
		public const long StandardRightsExecute = ReadControl;
		public const long StandardRightsAll = 0x001F0000L;

		public const long SpecificRightsAll = 0x0000FFFFL;
		public const long FileAllAccess = StandardRightsRequired | Synchronize | 0x1FF;

		public const long FileGenericRead = StandardRightsRead |
											FileReadData |
											FileReadAttributes |
											FileReadEa |
											Synchronize;

		public const long FileGenericWrite = StandardRightsWrite |
											 FileWriteData |
											 FileWriteAttributes |
											 FileWriteEa |
											 FileAppendData |
											 Synchronize;

		public const long FileGenericExecute = StandardRightsExecute |
											   FileReadAttributes |
											   FileExecute |
											   Synchronize;

		public const int FileShareNone = 0x00000000;
		public const int FileShareRead = 0x00000001;
		public const int FileShareWrite = 0x00000002;
		public const int FileShareDelete = 0x00000004;
		public const int FileAttributeReadonly = 0x00000001;
		public const int FileAttributeHidden = 0x00000002;
		public const int FileAttributeSystem = 0x00000004;
		public const int FileAttributeDirectory = 0x00000010;
		public const int FileAttributeArchive = 0x00000020;
		public const int FileAttributeDevice = 0x00000040;
		public const int FileAttributeNormal = 0x00000080;
		public const int FileAttributeTemporary = 0x00000100;
		public const int FileAttributeSparseFile = 0x00000200;
		public const int FileAttributeReparsePoint = 0x00000400;
		public const int FileAttributeCompressed = 0x00000800;
		public const int FileAttributeOffline = 0x00001000;
		public const int FileAttributeNotContentIndexed = 0x00002000;
		public const int FileAttributeEncrypted = 0x00004000;
		public const int FileNotifyChangeFileName = 0x00000001;
		public const int FileNotifyChangeDirName = 0x00000002;
		public const int FileNotifyChangeAttributes = 0x00000004;
		public const int FileNotifyChangeSize = 0x00000008;
		public const int FileNotifyChangeLastWrite = 0x00000010;
		public const int FileNotifyChangeLastAccess = 0x00000020;
		public const int FileNotifyChangeCreation = 0x00000040;
		public const int FileNotifyChangeSecurity = 0x00000100;
		public const int FileActionAdded = 0x00000001;
		public const int FileActionRemoved = 0x00000002;
		public const int FileActionModified = 0x00000003;
		public const int FileActionRenamedOldName = 0x00000004;
		public const int FileActionRenamedNewName = 0x00000005;
		public const int MailslotNoMessage = -1;
		public const int MailslotWaitForever = -1;
		public const int FileCaseSensitiveSearch = 0x00000001;
		public const int FileCasePreservedNames = 0x00000002;
		public const int FileUnicodeOnDisk = 0x00000004;
		public const int FilePersistentAcls = 0x00000008;
		public const int FileFileCompression = 0x00000010;
		public const int FileVolumeQuotas = 0x00000020;
		public const int FileSupportsSparseFiles = 0x00000040;
		public const int FileSupportsReparsePoints = 0x00000080;
		public const int FileSupportsRemoteStorage = 0x00000100;
		public const int FileVolumeIsCompressed = 0x00008000;
		public const int FileSupportsObjectIds = 0x00010000;
		public const int FileSupportsEncryption = 0x00020000;
		public const int FileNamedStreams = 0x00040000;
		public const int FileReadOnlyVolume = 0x00080000;
		public const int CreateAlways = 2;

		//Constants for errors:
		internal const uint ErrorFileNotFound = 2;
		internal const uint ErrorInvalidName = 123;
		internal const uint ErrorAccessDenied = 5;
		internal const uint ErrorIoPending = 997;

		//Constants for return value:
		internal const int InvalidHandleValue = -1;

		//Constants for dwFlagsAndAttributes:
		internal const uint FileFlagOverlapped = 0x40000000;

		//Constants for dwCreationDisposition:
		internal const uint OpenExisting = 3;

		//Constants for dwDesiredAccess:
		internal const uint GenericRead = 0x80000000;
		internal const uint GenericWrite = 0x40000000;

		public delegate void WriteFileCompletionDelegate(
			[In]uint dwErrorCode,
			[In]uint dwNumberOfBytesTransfered,
			[In]ref NativeOverlapped lpOverlapped);

		[DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
		public static extern void CopyMemory([In]IntPtr dest, [In]IntPtr src, [In]uint count);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern unsafe bool WriteFile(
			[In]IntPtr handle,
			[In]byte* lpBuffer,
			[In]uint nNumberOfBytesToWrite,
			[Out]out uint lpNumberOfBytesWritten,
			[In]IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern unsafe bool WriteFileEx(
			[In]IntPtr handle,
			[In]byte* lpBuffer,
			[In]uint nNumberOfBytesToWrite,
			[In]IntPtr lpOverlapped,
			[In]WriteFileCompletionDelegate lpCompletionRoutine);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern unsafe bool ReadFile(
			[In]IntPtr hFile,
			[Out]byte* lpBuffer,
			[In]uint nNumberOfBytesToRead,
			[Out]out uint lpNumberOfBytesRead,
			[In]IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr CreateFile(
			[In]string lpFileName,
			[In]uint dwDesiredAccess,
			[In]uint dwShareMode,
			[In]IntPtr lpSecurityAttributes,
			[In]uint dwCreationDisposition,
			[In]uint dwFlagsAndAttributes,
			[In]IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool CloseHandle([In]IntPtr hObject);
	}
}