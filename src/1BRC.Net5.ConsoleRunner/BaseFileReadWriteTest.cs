using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace _1BRC.Net5.ConsoleRunner {
	internal abstract class BaseFileReadWriteTest {
		private const int TestRunCount = 12;
		protected const int FileSizeToWrite = 512 * 58594; // 30000128
		protected const string TestFile = "FileWriteTests.txt";
		private const string ResultFile = "resultFile.txt";

		protected static readonly int[] ChunkAndBufferSizes = {
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
			4194304,
			8388608,
			16777216,
			33554432
		};

		protected static readonly FileOptions[] ReadOptions = {
			FileOptions.None,
			FileOptions.SequentialScan,
			FileOptions.WriteThrough,
			FileOptions.SequentialScan | FileOptions.WriteThrough
		};

		protected static readonly FileOptions[] WriteOptions = {
			FileOptions.None,
			FileOptions.SequentialScan
		};

		#region

		private readonly string _netVersion;

		#endregion

		protected BaseFileReadWriteTest(string netVersion) {
			_netVersion = netVersion;
		}

		public void ExecuteTest(Action<string> progressCallback) {
			var sw = new Stopwatch();
			sw.Start();
			var writeTableGenerator = new ResultTableGenerator();
			var readTableGenerator = new ResultTableGenerator();
			var fileContentBytes = new byte[FileSizeToWrite];
			var random = new Random();
			random.NextBytes(fileContentBytes);

			InternalExecuteTest(progressCallback, fileContentBytes, writeTableGenerator, readTableGenerator);

			DeleteFile();
			sw.Stop();

			using (var writer = new StreamWriter(ResultFile)) {
				writer.WriteLine(_netVersion);
				writer.WriteLine($"File read & write test with {Math.Round(FileSizeToWrite / 1000.0 / 1000.0, 3)} MB done in {sw.Elapsed:hh':'mm':'ss':'fff}!");
				writer.WriteLine(Environment.NewLine);
				writer.WriteLine(Environment.NewLine);
				writer.WriteLine(writeTableGenerator.PrintTable("Write", FileSizeToWrite));
				writer.WriteLine(Environment.NewLine);
				writer.WriteLine(readTableGenerator.PrintTable("Read", FileSizeToWrite));
			}
		}

		protected abstract void InternalExecuteTest(Action<string> progressCallback, byte[] fileContentBytes, ResultTableGenerator writeTableGenerator, ResultTableGenerator readTableGenerator);

		protected static void DeleteFile() => File.Delete(TestFile);

		protected static TimeSpan? RunTest(Action testAction, Action cleanupAction = null) {
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