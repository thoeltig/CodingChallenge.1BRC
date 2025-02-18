using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using _1BRC.Net5.ConsoleRunner;

namespace _1BRC.ConsoleRunner {
	internal static class FileReadAndWrite {
		private const int TestRunCount = 12;
		private const int FileSizeToWrite = 20000000;
		private const string TestFile = "FileWriteTests.txt";
		private const string ResultFile = "resultFile.txt";

		private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;

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

		private static readonly FileOptions[] _options = {
			FileOptions.None,
			FileOptions.SequentialScan,
			FileOptions.WriteThrough,
			FileOptions.SequentialScan | FileOptions.WriteThrough,
			FileOptions.WriteThrough | FileFlagNoBuffering,
			FileOptions.SequentialScan | FileOptions.WriteThrough | FileFlagNoBuffering
		};

		public static void ExecuteTest(Action<string> progressCallback) {
			var writeTableGenerator = new ResultTableGenerator();
			var readTableGenerator = new ResultTableGenerator();
			var fileContentBytes = new byte[FileSizeToWrite];
			var random = new Random();
			random.NextBytes(fileContentBytes);

			foreach (var option in _options) {
				progressCallback($"Start file option {option}");

				foreach (var chunkSize in _chunkSizes) {
					var byteArrays = SplitIntoByteArrays(fileContentBytes, chunkSize);
					var splitCount = byteArrays.Length;

					foreach (var bufferSize in _chunkSizes) {
						var time = RunTest(() => {
							using (var stream = new FileStream(TestFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, option)) {
								foreach (var line in byteArrays) {
									stream.Write(line, 0, line.Length);
								}
							}
						}, DeleteFile);
						writeTableGenerator.Add(option, chunkSize, bufferSize, time);

						time = RunTest(() => {
							using (var stream = new FileStream(TestFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, option)) {
								var buffer = new byte[chunkSize];
								while (stream.Read(buffer, 0, buffer.Length) != 0) {
								}
							}
						});
						readTableGenerator.Add(option, chunkSize, bufferSize, time);
					}
				}
			}

			DeleteFile();

			using (var writer = new StreamWriter(ResultFile)) {
				writer.WriteLine(writeTableGenerator.PrintTable("Write", FileSizeToWrite));
				writer.WriteLine(Environment.NewLine);
				writer.WriteLine(readTableGenerator.PrintTable("Read", FileSizeToWrite));
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

		private static TimeSpan? RunTest(Action testAction, Action cleanupAction = null) {
			var sw = new Stopwatch();
			var times = new TimeSpan[TestRunCount];

			try {
				for (var i = 0; i < TestRunCount; i++) {
					cleanupAction?.Invoke();

					sw.Restart();
					testAction();
					sw.Stop();

					times[i] = sw.Elapsed;
				}

				var quartile = (int)Math.Round(TestRunCount * 0.25);
				var half = (int)Math.Round(TestRunCount * 0.5);
				return new TimeSpan(times.OrderBy(x => x).Skip(quartile).Take(half).Sum(x => x.Ticks) / half);
			}
			catch {
				return null;
			}
		}
	}
}