using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace _1BRC.ConsoleRunner {
	internal static class FileReadAndWrite {
		private const int TestRunCount = 12;
		private const int FileSizeToWrite = 20000000;
		private const string TestFile = "FileWriteTests.txt";
		private const string ResultFile = "resultFile.txt";

		private static readonly int[] _chunkSizes = {
			1024,
			2048,
			4096,
			8192,
			16384,
			32768,
			65536,
			131072,
			262144
		};

		private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;

		private static readonly FileOptions[] _options = {
			FileOptions.None,
			FileOptions.SequentialScan,
			FileOptions.WriteThrough,
			FileOptions.SequentialScan | FileOptions.WriteThrough,
			FileOptions.WriteThrough | FileFlagNoBuffering,
			FileOptions.SequentialScan | FileOptions.WriteThrough | FileFlagNoBuffering,
		};

		public static void ExecuteTest(Action<string> progressCallback) {
			using (var logWriter = new StreamWriter(ResultFile)) {
				var fileContentBytes = new byte[FileSizeToWrite];
				var random = new Random();
				random.NextBytes(fileContentBytes);

				logWriter.WriteLine($"Total content size: {FileSizeToWrite}");

				foreach (var option in _options) {
					WriteMultipleEmptyLines(logWriter);
					logWriter.WriteLine($"File option {option}");
					progressCallback($"Start file option {option}");

					foreach (var chunkSize in _chunkSizes) {
						WriteMultipleEmptyLines(logWriter);

						var byteArrays = SplitIntoByteArrays(fileContentBytes, chunkSize);
						var splitCount = byteArrays.Length;
						logWriter.WriteLine($"Requires {splitCount} read & write operations which will each transfer {chunkSize} bytes");

						WriteMultipleEmptyLines(logWriter);

						foreach (var bufferSize in _chunkSizes) {
							WriteMultipleEmptyLines(logWriter);
							logWriter.WriteLine($"Set buffer size to {bufferSize}");
							WriteMultipleEmptyLines(logWriter);

							RunTest("FileStream.Write", () => {
								using (var stream = new FileStream(TestFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, option)) {
									foreach (var line in byteArrays) {
										stream.Write(line, 0, line.Length);
									}
								}
							}, logWriter, DeleteFile);

							RunTest("FileStream.Read", () => {
								using (var stream = new FileStream(TestFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, option)) {
									var buffer = new byte[chunkSize];
									while (stream.Read(buffer, 0, buffer.Length) != 0) {
									}
								}
							}, logWriter);
						}
					}
				}
			}

			DeleteFile();
			return;

			void WriteMultipleEmptyLines(StreamWriter logWriter) {
				logWriter.WriteLine();
				logWriter.WriteLine();
			}
		}

		private static void DeleteFile() => File.Delete(TestFile);

		private static byte[][] SplitIntoByteArrays(byte[] fileContentAsBytes, int chunkSize) {
			var contentLength = fileContentAsBytes.Length;
			var splitCount = (int)Math.Ceiling(contentLength / (double)chunkSize);
			var output = new byte[splitCount][];
			for (int contentIdx = 0, outputIdx = 0; contentIdx < contentLength && outputIdx < splitCount; contentIdx += chunkSize, outputIdx++) {
				var copyLength = contentLength - contentIdx;
				if (copyLength > chunkSize) {
					copyLength = chunkSize;
				}

				var array = new byte[copyLength];
				Array.Copy(fileContentAsBytes, contentIdx, array, 0, copyLength);
				output[outputIdx] = array;
			}

			return output;
		}

		private static void RunTest(string testName, Action testAction, StreamWriter resultWriter, Action cleanupAction = null) {
			var sw = new Stopwatch();
			var times = new TimeSpan[TestRunCount];

			resultWriter.WriteLine($"Test case: {testName}");

			try {
				for (var i = 0; i < TestRunCount; i++) {
					cleanupAction?.Invoke();

					sw.Restart();
					testAction();
					sw.Stop();

					times[i] = sw.Elapsed;
				}

				var avg = new TimeSpan(times.Sum(x => x.Ticks) / TestRunCount);
				resultWriter.WriteLine($"Average: {avg:ss':'fffffff}");

				var quartile = (int)Math.Round(TestRunCount * 0.25);
				var half = (int)Math.Round(TestRunCount * 0.5);
				var median = new TimeSpan(times.OrderBy(x => x).Skip(quartile).Take(half).Sum(x => x.Ticks) / half);
				resultWriter.WriteLine($"Median: {median:ss':'fffffff}");
			}
			catch {
				resultWriter.WriteLine("Test failed...");
			}

			resultWriter.WriteLine();
		}
	}
}