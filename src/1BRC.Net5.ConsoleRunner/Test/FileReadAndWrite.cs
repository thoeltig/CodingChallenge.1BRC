using System;
using System.IO;

namespace _1BRC.Net5.ConsoleRunner.Test {
	internal class FileReadAndWrite : BaseFileReadWriteTest {
		public FileReadAndWrite()
			: base(".NET 5") {
		}

		protected override void InternalExecuteTest(Action<string> progressCallback, byte[] fileContentBytes, ResultTableGenerator writeTableGenerator, ResultTableGenerator readTableGenerator) {
			foreach (var chunkSize in ChunkAndBufferSizes) {
				progressCallback($"Chunk size {chunkSize}");
				var splitCount = (int)Math.Ceiling(FileSizeToWrite / (double)chunkSize);

				foreach (var bufferSize in ChunkAndBufferSizes) {
					foreach (var option in WriteOptions) {
						var time = RunTest(() => Write(bufferSize, option, splitCount, chunkSize, fileContentBytes.AsSpan()), DeleteFile);
						writeTableGenerator.Add(option, chunkSize, bufferSize, time);
					}

					foreach (var option in ReadOptions) {
						var time = RunTest(() => Read(bufferSize, option, chunkSize));
						readTableGenerator.Add(option, chunkSize, bufferSize, time);
					}
				}
			}
		}

		private static void Write(int bufferSize, FileOptions option, int splitCount, int chunkSize, ReadOnlySpan<byte> content) {
			var contentLength = content.Length;
			using (var stream = new FileStream(TestFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, option)) {
				for (var i = 0; i < splitCount; i++) {
					var contentIdx = i * chunkSize;
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
	}
}