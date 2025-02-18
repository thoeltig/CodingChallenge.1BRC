using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace _1BRC.Net5.ConsoleRunner {
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
			var writeTableGenerator = new ResultTableGenerator();
			var readTableGenerator = new ResultTableGenerator();

			var fileContentBytes = new byte[FileSizeToWrite];
			var random = new Random();
			random.NextBytes(fileContentBytes);
			var chunksCount = _chunkSizes.Length;

			foreach (var option in _options) {
				progressCallback($"Start file option {option}");

				for (var j = 0; j < chunksCount; j++) {
					var chunkSize = _chunkSizes[j];
					var splitCount = (int)Math.Ceiling(FileSizeToWrite / (double)chunkSize);

					for (var k = 0; k < chunksCount; k++) {
						var bufferSize = _chunkSizes[k];

						var time = RunTest(() => Write(bufferSize, option, splitCount, chunkSize, fileContentBytes.AsSpan()), DeleteFile);
						writeTableGenerator.Add(option, chunkSize, bufferSize, time);

						time = RunTest(() => Read(bufferSize, option, chunkSize));
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

		private static void Write(int bufferSize, FileOptions option, int splitCount, int chunkSize, ReadOnlySpan<byte> content) {
			var contentLength = content.Length;
			using (var stream = new FileStream(TestFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, option)) {
				for (int contentIdx = 0, outputIdx = 0; contentIdx < contentLength && outputIdx < splitCount; contentIdx += chunkSize, outputIdx++) {
					var copyLength = contentLength - contentIdx;
					if (copyLength > chunkSize) {
						copyLength = chunkSize;
					}

					var slice = content.Slice(contentIdx, copyLength);
					stream.Write(slice);
				}
			}
		}

		private static void Read(int bufferSize, FileOptions option, int chunkSize) {
			using (var stream = new FileStream(TestFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, option)) {
				var buffer = new Span<byte>(new byte[chunkSize]);
				var readBytes = 0;
				while ((readBytes = stream.Read(buffer)) != 0) {
				}
			}
		}

		private static void DeleteFile() => File.Delete(TestFile);

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