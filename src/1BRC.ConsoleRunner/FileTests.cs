using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace _1BRC.ConsoleRunner {
	internal static class FileTests {
		private const int TestRunCount = 12;
		private const int FileSizeToWrite = 10000000;
		private const string TestFile = "FileWriteTests.txt";
		private const string ResultFile = "resultFile.txt";

		private static readonly int[] _writeSizes = {
			4096,
			8192,
			16384,
			32768,
			65536
		};

		public static void Write() {
			using (var logWriter = new StreamWriter(ResultFile)) {
				var fileContentBytes = new byte[FileSizeToWrite];
				var random = new Random();
				random.NextBytes(fileContentBytes);

				//File.CreateText
				var fileContentAsString = Encoding.UTF8.GetString(fileContentBytes);
				var testName = $"File.CreateText + StreamWriter.Write one giant string (length {FileSizeToWrite})";
				RunTest(testName, () => {
					using (var stream = File.CreateText(TestFile)) {
						stream.Write(fileContentAsString);
					}
				}, DeleteFile, logWriter);

				foreach (var writeSize in _writeSizes) {
					var list = SplitFileContentIntoStrings(writeSize);
					testName = $"File.CreateText + StreamWriter.Write {list.Count} strings (line length {writeSize})";
					RunTest(testName, () => {
						using (var stream = File.CreateText(TestFile)) {
							foreach (var line in list) {
								stream.Write(line);
							}
						}
					}, DeleteFile, logWriter);
				}

				var charArray = fileContentAsString.ToArray();
				testName = $"File.CreateText + StreamWriter.Write one giant char array (length {FileSizeToWrite})";
				RunTest(testName, () => {
					using (var stream = File.CreateText(TestFile)) {
						stream.Write(charArray);
					}
				}, DeleteFile, logWriter);

				foreach (var writeSize in _writeSizes) {
					var list = SplitFileContentIntoCharArrays(writeSize);
					testName = $"File.CreateText + StreamWriter.Write {list.Count} char arrays (length {writeSize})";
					RunTest(testName, () => {
						using (var stream = File.CreateText(TestFile)) {
							foreach (var line in list) {
								stream.Write(line);
							}
						}
					}, DeleteFile, logWriter);
				}

				// FileInfo.CreateTexts
				testName = $"FileInfo.CreateText + StreamWriter.Write one giant string (length {FileSizeToWrite})";
				RunTest(testName, () => {
					var info = new FileInfo(TestFile);
					using (var stream = info.CreateText()) {
						stream.Write(fileContentAsString);
					}
				}, DeleteFile, logWriter);

				foreach (var writeSize in _writeSizes) {
					var list = SplitFileContentIntoStrings(writeSize);
					testName = $"FileInfo.CreateText + StreamWriter.Write {list.Count} strings (line length {writeSize})";
					RunTest(testName, () => {
						var info = new FileInfo(TestFile);
						using (var stream = info.CreateText()) {
							foreach (var line in list) {
								stream.Write(line);
							}
						}
					}, DeleteFile, logWriter);
				}

				testName = $"FileInfo.CreateText + StreamWriter.Write one giant char array (length {FileSizeToWrite})";
				RunTest(testName, () => {
					var info = new FileInfo(TestFile);
					using (var stream = info.CreateText()) {
						stream.Write(charArray);
					}
				}, DeleteFile, logWriter);

				foreach (var writeSize in _writeSizes) {
					var list = SplitFileContentIntoCharArrays(writeSize);
					testName = $"FileInfo.CreateText + StreamWriter.Write {list.Count} char arrays (length {writeSize})";
					RunTest(testName, () => {
						var info = new FileInfo(TestFile);
						using (var stream = info.CreateText()) {
							foreach (var line in list) {
								stream.Write(line);
							}
						}
					}, DeleteFile, logWriter);
				}

				// StreamWriter
				testName = $"StreamWriter.Write one giant string (length {FileSizeToWrite})";
				RunTest(testName, () => {
					using (var stream = new StreamWriter(TestFile)) {
						stream.Write(fileContentAsString);
					}
				}, DeleteFile, logWriter);

				testName = $"StreamWriter.Write one giant char array (length {FileSizeToWrite})";
				RunTest(testName, () => {
					using (var stream = new StreamWriter(TestFile)) {
						stream.Write(charArray);
					}
				}, DeleteFile, logWriter);

				foreach (var writeSize in _writeSizes) {
					var list = SplitFileContentIntoStrings(writeSize);
					testName = $"StreamWriter.Write {list.Count} strings (length {writeSize})";
					RunTest(testName, () => {
						using (var stream = new StreamWriter(TestFile)) {
							foreach (var line in list) {
								stream.Write(line);
							}
						}
					}, DeleteFile, logWriter);
				}

				foreach (var writeSize in _writeSizes) {
					var list = SplitFileContentIntoCharArrays(writeSize);
					testName = $"StreamWriter.Write {list.Count} char arrays (length {writeSize})";
					RunTest(testName, () => {
						using (var stream = new StreamWriter(TestFile)) {
							foreach (var line in list) {
								stream.Write(line, 0, line.Length);
							}
						}
					}, DeleteFile, logWriter);
				}
				
				// File.Create
				foreach (var writeSize in _writeSizes) {
					var list = SplitFileContentIntoByteArrays(writeSize);
					foreach (var bufferSize in _writeSizes) {
						testName = $"File.Create + FileStream.Write with buffer size {bufferSize} a total of {list.Count} byte arrays (length {writeSize})";
						RunTest(testName, () => {
							using (var stream = File.Create(TestFile, bufferSize)) {
								foreach (var line in list) {
									stream.Write(line, 0, line.Length);
								}
							}
						}, DeleteFile, logWriter);
					}
				}

				// FileStream
				foreach (var writeSize in _writeSizes) {
					var list = SplitFileContentIntoByteArrays(writeSize);
					foreach (var bufferSize in _writeSizes) {
						testName = $"FileStream.Write with buffer size {bufferSize} a total of {list.Count} byte arrays (length {writeSize})";
						RunTest(testName, () => {
							using (var stream = new FileStream(TestFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize)) {
								foreach (var line in list) {
									stream.Write(line, 0, line.Length);
								}
							}
						}, DeleteFile, logWriter);
					}
				}

                DeleteFile();
				return;

				List<string> SplitFileContentIntoStrings(int writeSize) {
					var list = new List<string>();
					for (var i = 0; i < FileSizeToWrite; i += writeSize) {
						list.Add(Encoding.UTF8.GetString(fileContentBytes.Skip(i).Take(writeSize).ToArray()));
					}

					return list;
				}

				List<char[]> SplitFileContentIntoCharArrays(int writeSize) {
					var list = new List<char[]>();
					for (var i = 0; i < FileSizeToWrite; i += writeSize) {
						list.Add(fileContentBytes.Skip(i).Take(writeSize).Select(x => (char)x).ToArray());
					}

					return list;
				}

				List<byte[]> SplitFileContentIntoByteArrays(int writeSize) {
					var list = new List<byte[]>();
					for (var i = 0; i < FileSizeToWrite; i += writeSize) {
						list.Add(fileContentBytes.Skip(i).Take(writeSize).ToArray());
					}

					return list;
				}

				void DeleteFile() => File.Delete(TestFile);
			}
		}

		private static void RunTest(string testName, Action testAction, Action cleanupAction, StreamWriter resultWriter) {
			var sw = new Stopwatch();
			var times = new TimeSpan[TestRunCount];

			resultWriter.WriteLine($"Test case: {testName}");

			for (var i = 0; i < TestRunCount; i++) {
				cleanupAction();

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