using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace _1BRC.ConsoleRunner {
	internal static class FileTests {
		private const int TestRunCount = 12;
		private const int FileSizeToWrite = 15000000;
		private const string TestFile = "FileWriteTests.txt";
		private const string ResultFile = "resultFile.txt";

		private static readonly int[] _writeSizes = {
			4096,
			4096*2,
			4096*2*2,
			4096*2*2*2,
			4096*2*2*2*2,
			4096*2*2*2*2*2,
			4096*2*2*2*2*2*2,
		};

		public static void Write() {
			using (var logWriter = new StreamWriter(ResultFile)) {
				var fileContentBytes = new byte[FileSizeToWrite];
				var random = new Random();
				random.NextBytes(fileContentBytes);

				foreach (var writeSize in _writeSizes) {
					// Write bytes
					var byteArrays = SplitIntoByteArrays(fileContentBytes, writeSize);
					var splitCount = byteArrays.Length;
					logWriter.WriteLine($"Content split into {splitCount} parts with a write size of {writeSize}");
					WriteMultipleEmptyLines(logWriter);
					logWriter.WriteLine("Write as byte arrays");

					foreach (var bufferSize in _writeSizes) {
						WriteMultipleEmptyLines(logWriter);
						logWriter.WriteLine($"Set buffer size to {bufferSize}");
						WriteMultipleEmptyLines(logWriter);

						RunTest("File.Create + FileStream.Write", () => {
							using (var stream = File.Create(TestFile, bufferSize)) {
								foreach (var line in byteArrays) {
									stream.Write(line, 0, line.Length);
								}
							}
						}, logWriter);

						RunTest("FileStream.Write", () => {
							using (var stream = new FileStream(TestFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize)) {
								foreach (var line in byteArrays) {
									stream.Write(line, 0, line.Length);
								}
							}
						}, logWriter);
					}

					// Write chars
					var charArrays = ConvertToCharArrays(byteArrays);

					WriteMultipleEmptyLines(logWriter);
					logWriter.WriteLine("Write as char arrays");
					WriteMultipleEmptyLines(logWriter);

					RunTest("File.CreateText + StreamWriter.Write", () => {
						using (var stream = File.CreateText(TestFile)) {
							foreach (var line in charArrays) {
								stream.Write(line);
							}
						}
					}, logWriter);

					RunTest("FileInfo.CreateText + StreamWriter.Write", () => {
						var info = new FileInfo(TestFile);
						using (var stream = info.CreateText()) {
							foreach (var line in charArrays) {
								stream.Write(line);
							}
						}
					}, logWriter);

					RunTest("StreamWriter.Write", () => {
						using (var stream = new StreamWriter(TestFile)) {
							foreach (var line in charArrays) {
								stream.Write(line);
							}
						}
					}, logWriter);

					// Write strings
					var strings = ConvertToStrings(charArrays);

					WriteMultipleEmptyLines(logWriter);
					logWriter.WriteLine("Write as strings");
					WriteMultipleEmptyLines(logWriter);

					RunTest("File.CreateText + StreamWriter.Write", () => {
						using (var stream = File.CreateText(TestFile)) {
							foreach (var line in strings) {
								stream.Write(line);
							}
						}
					}, logWriter);

					RunTest("FileInfo.CreateText + StreamWriter.Write", () => {
						var info = new FileInfo(TestFile);
						using (var stream = info.CreateText()) {
							foreach (var line in strings) {
								stream.Write(line);
							}
						}
					}, logWriter);

					RunTest("StreamWriter.Write", () => {
						using (var stream = new StreamWriter(TestFile)) {
							foreach (var line in strings) {
								stream.Write(line);
							}
						}
					}, logWriter);
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
			}

			return output;
		}

		private static char[][] ConvertToCharArrays(byte[][] input) {
			var size = input.Length;
			var output = new char[size][];
			for (var i = 0; i < size; i++) {
				var bytes = input[i];
				var count = bytes.Length;
				var chars = new char[count];
				for (var j = 0; j < count; j++) {
					chars[j] = (char)bytes[j];
				}

				output[i] = chars;
			}

			return output;
		}

		private static string[] ConvertToStrings(char[][] input) {
			var size = input.Length;
			var output = new string[size];
			for (var i = 0; i < size; i++) {
				output[i] = new string(input[i]);
			}

			return output;
		}

		private static void RunTest(string testName, Action testAction, StreamWriter resultWriter) {
			var sw = new Stopwatch();
			var times = new TimeSpan[TestRunCount];

			resultWriter.WriteLine($"Test case: {testName}");

			for (var i = 0; i < TestRunCount; i++) {
				DeleteFile();

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

			resultWriter.WriteLine();
		}
	}
}