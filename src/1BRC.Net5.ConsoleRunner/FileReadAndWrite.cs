using System;
using System.Diagnostics;
using System.Globalization;
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

		private static readonly int _optionsCount = _options.Length;
		private static readonly TimeSpan[][][] _outputWrite = new TimeSpan[_optionsCount][][];
		private static readonly TimeSpan[][][] _outputRead = new TimeSpan[_optionsCount][][];

		public static void ExecuteTest(Action<string> progressCallback) {
			var fileContentBytes = new byte[FileSizeToWrite];
			var random = new Random();
			random.NextBytes(fileContentBytes);
			var chunksCount = _chunkSizes.Length;

			for (var i = 0; i < _optionsCount; i++) {
				var option = _options[i];
				_outputWrite[i] = new TimeSpan[chunksCount][];
				_outputRead[i] = new TimeSpan[chunksCount][];
				progressCallback($"Start file option {option}");

				for (var j = 0; j < chunksCount; j++) {
					var chunkSize = _chunkSizes[j];
					_outputWrite[i][j] = new TimeSpan[chunksCount];
					_outputRead[i][j] = new TimeSpan[chunksCount];
					var splitCount = (int)Math.Ceiling(FileSizeToWrite / (double)chunkSize);

					for (var k = 0; k < chunksCount; k++) {
						var bufferSize = _chunkSizes[k];
						_outputWrite[i][j][k] = RunTest(() => Write(bufferSize, option, splitCount, chunkSize, fileContentBytes.AsSpan()), DeleteFile);
						_outputRead[i][j][k] = RunTest(() => Read(bufferSize, option, chunkSize));
					}
				}
			}

			DeleteFile();

			using (var writer = new StreamWriter(ResultFile)) {
				foreach (var line in GenerateResultTable("Write", _outputWrite)) {
					writer.WriteLine(line);
				}

				writer.WriteLine(Environment.NewLine);
				writer.WriteLine(Environment.NewLine);

				foreach (var line in GenerateResultTable("Read", _outputRead)) {
					writer.WriteLine(line);
				}
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

		private static TimeSpan RunTest(Action testAction, Action cleanupAction = null) {
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

				//var avg = new TimeSpan(times.Sum(x => x.Ticks) / TestRunCount);
				var quartile = (int)Math.Round(TestRunCount * 0.25);
				var half = (int)Math.Round(TestRunCount * 0.5);
				var median = new TimeSpan(times.OrderBy(x => x).Skip(quartile).Take(half).Sum(x => x.Ticks) / half);
				return median;
			}
			catch {
				return TimeSpan.Zero;
			}
		}

		private static string[] GenerateResultTable(string tableName, TimeSpan[][][] values) {
			const int headerSize = 2;
			double? firstTime = null;
			var lines = new string[_chunkSizes.Length * _chunkSizes.Length + headerSize];
			lines[0] = $"|{tableName}|";
			lines[1] = "|----|";
			for (var i = 0; i < values.Length; i++) {
				var allValuesForOption = values[i];
				var option = _options[i];
				lines[0] = string.Concat(lines[0], option, "|%|");
				lines[1] = string.Concat(lines[1], "----|----|");

				for (var j = 0; j < allValuesForOption.Length; j++) {
					var allBuffersPerChunkSize = allValuesForOption[j];
					var bufferSize = _chunkSizes[j];

					for (var k = 0; k < allBuffersPerChunkSize.Length; k++) {
						var timeSpanPerChunk = allBuffersPerChunkSize[k];
						var chunkSize = _chunkSizes[k];
						var splitCount = (int)Math.Ceiling(FileSizeToWrite / (double)chunkSize);
						var lineIdx = allBuffersPerChunkSize.Length * j + k + headerSize;

						if (i == 0) {
							lines[lineIdx] = string.Concat($"|Buffer {bufferSize} Chunk {chunkSize} ({splitCount} times)|");

							if (firstTime.HasValue == false) {
								firstTime = timeSpanPerChunk.TotalSeconds;
							}
						}

						lines[lineIdx] = string.Concat(lines[lineIdx], timeSpanPerChunk.ToString("ss':'fffffff"), "|", Math.Round((timeSpanPerChunk.TotalSeconds - firstTime.Value) / firstTime.Value, 2).ToString(CultureInfo.InvariantCulture), "%|");
					}
				}
			}

			return lines;
		}
	}
}