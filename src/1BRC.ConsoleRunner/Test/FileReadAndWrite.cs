using System;
using System.IO;
using _1BRC.Net5.ConsoleRunner.Test;

namespace _1BRC.ConsoleRunner.Test {
	internal class FileReadAndWrite : BaseFileReadWriteTest {
		public FileReadAndWrite()
			: base(".NET Framework 4.7.2") {
		}

		protected override void InternalExecuteTest(Action<string> progressCallback, byte[] fileContentBytes, ResultTableGenerator writeTableGenerator, ResultTableGenerator readTableGenerator) {
			foreach (var chunkSize in ChunkAndBufferSizes) {
				progressCallback($"Chunk size {chunkSize}");
				var byteArrays = SplitIntoByteArrays(fileContentBytes, chunkSize);

				foreach (var bufferSize in ChunkAndBufferSizes) {
					foreach (var option in WriteOptions) {
						var time = RunTest(() => {
							using (var stream = new FileStream(TestFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, option)) {
								foreach (var line in byteArrays) {
									stream.Write(line, 0, line.Length);
								}
							}
						}, DeleteFile);
						writeTableGenerator.Add(option, chunkSize, bufferSize, time);
					}

					foreach (var option in ReadOptions) {
						var time = RunTest(() => {
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
		}

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
	}
}