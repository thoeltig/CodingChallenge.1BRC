using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace _1BRC.Net5.ConsoleRunner {
	internal static class FileReadAndWrite {
		private const int TestRunCount = 12;
		private const int FileSizeToWrite = 512 * 58594; // 30000128
		private const string TestFile = "FileWriteTests.txt";
		private const string ResultFile = "resultFile.txt";

		private static readonly int[] _chunkSizes = {
			131072,
			262144,
			524288,
			1048576,
			2097152,
			4194304
		};

		private static readonly int[] _bufferSizes = {
			1024,
			2048,
			4096,
			8192,
			16384,
			32768,
			65536,
			131072,
			262144,
			524288,
			1048576,
			2097152,
			4194304
		};

		private static readonly FileOptions[] _readOptions = {
			FileOptions.None,
			FileOptions.SequentialScan,
			FileOptions.WriteThrough,
			FileOptions.SequentialScan | FileOptions.WriteThrough,
		};

		private static readonly FileOptions[] _writeOptions = {
			FileOptions.None,
			FileOptions.SequentialScan,
		};

		public static void ExecuteTest(Action<string> progressCallback) {
			var sw = new Stopwatch();
			sw.Start();
			var writeTableGenerator = new ResultTableGenerator();
			var readTableGenerator = new ResultTableGenerator();

			var fileContentBytes = new byte[FileSizeToWrite];
			var random = new Random();
			random.NextBytes(fileContentBytes);
			
			foreach (var chunkSize in _chunkSizes) {
				progressCallback($"Chunk size {chunkSize}");
				var splitCount = (int)Math.Ceiling(FileSizeToWrite / (double)chunkSize);

				foreach (var bufferSize in _bufferSizes) {
					foreach (var option in _writeOptions) {
						var time = RunTest(() => Write(bufferSize, option, splitCount, chunkSize, fileContentBytes.AsSpan()), DeleteFile);
						writeTableGenerator.Add(option, chunkSize, bufferSize, time);
					}

					foreach (var option in _readOptions) {
						var time = RunTest(() => Read(bufferSize, option, chunkSize));
						readTableGenerator.Add(option, chunkSize, bufferSize, time);
					}
				}
			}
			
			DeleteFile();
			sw.Stop();

			using (var writer = new StreamWriter(ResultFile)) {
				writer.WriteLine(".NET (Core) 5");
				writer.WriteLine($"File read & write test with {Math.Round(FileSizeToWrite / 1000.0 / 1000.0, 3)} MB done in {sw.Elapsed:hh':'mm':'ss':'fff}!");
				writer.WriteLine(Environment.NewLine);
				writer.WriteLine(Environment.NewLine);
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