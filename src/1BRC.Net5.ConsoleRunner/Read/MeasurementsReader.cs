using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace _1BRC.Net5.ConsoleRunner.Read {
	internal static class MeasurementsReader {
		public static IReadOnlyCollection<TemperatureContainer> ReadFile(string filePath) {
			#if DEBUG
			int maxParallel = 1;
			#else
			var maxParallel = Environment.ProcessorCount;
			#endif

			const int blockSize = 131016;
			const int lineSize = 106;
			var tasks = new Task<FirstAndLastPart>[maxParallel];
			var allBlocks = new byte[blockSize * maxParallel].AsSpan();
			var lines = new byte[lineSize * maxParallel].AsSpan();

			var offset = 0;
			var isRunning = true;
			var defaultPartsTask = Task.FromResult((FirstAndLastPart)default);
			var dic = new Dictionary<uint, TemperatureContainer>();
			using (var reader = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 8192, FileOptions.SequentialScan | FileOptions.WriteThrough)) {
				do {
					for (var i = 0; i < maxParallel; i++) {
						var block = allBlocks.Slice(i * blockSize + offset, blockSize - offset);
						int readBytes;
						if ((readBytes = reader.Read(block)) != 0) {
							isRunning = readBytes == blockSize - offset;
							tasks[i] = ReadLinesAsync(isRunning, dic, allBlocks.Slice(i * blockSize, readBytes + offset), readBytes + offset, lines.Slice(i * lineSize, lineSize));
							offset = 0;
						} else {
							tasks[i] = defaultPartsTask;
						}
					}

					Task.WaitAll(tasks);

					offset = 0;
					for (var i = 0; i < maxParallel; i++) {
						var parts = tasks[i].Result;
						var blockStartIdx = i * blockSize;

						if (parts.FirstPartCount != 0) {
							allBlocks.Slice(blockStartIdx, parts.FirstPartCount).CopyTo(allBlocks.Slice(offset, parts.FirstPartCount));
							offset += parts.FirstPartCount;
						}

						if (parts.LastPartCount != 0) {
							allBlocks.Slice(blockStartIdx + parts.LastPartIndex, parts.LastPartCount).CopyTo(allBlocks.Slice(offset, parts.LastPartCount));
							offset += parts.LastPartCount;
						}
					}
				} while (isRunning);
			}

			Array.Clear(tasks, 0, tasks.Length);
			return dic.Values;
		}

		private static Task<FirstAndLastPart> ReadLinesAsync(bool ignoreStartAndEndPart, Dictionary<uint, TemperatureContainer> dic, ReadOnlySpan<byte> block, int readBytes, Span<byte> linePtr) {
			const byte lineSeparator = 59; // ;
			const byte signedSymbol = 45; // -
			const byte temperatureSeparator = 46; // .
			const byte newLine = 10; // \n

			var idx = 0;
			var nameLength = 0;
			var flag = IntParseFlag.None;
			var temperature = 0;
			int? firstLineEndIdx = null;
			int? lastLineStartIdx = null;

			for (var bufferIdx = 0; bufferIdx < readBytes; bufferIdx++) {
				var value = block[bufferIdx];

				switch (value) {
					case lineSeparator: {
						nameLength = idx;
						break;
					}
					case temperatureSeparator: {
						flag |= IntParseFlag.HasDot;
						break;
					}
					case signedSymbol: {
						flag |= IntParseFlag.Signed;
						break;
					}
					case newLine: {
						if (firstLineEndIdx.HasValue == false && ignoreStartAndEndPart) {
							firstLineEndIdx = bufferIdx;
							idx = 0;
							flag = IntParseFlag.None;
							continue;
						}

						lastLineStartIdx = bufferIdx;

						// get key with hash logic
						var key = 2166136261;
						unchecked {
							for (var i = 0; i < nameLength; i++) {
								key = (key * 16777619) ^ linePtr[i];
							}
						}

						// parse int -> double or single digit with or without fraction
						switch (flag) {
							case IntParseFlag.Signed | IntParseFlag.HasDot: {
								temperature = -((idx - nameLength) switch {
									3 => (linePtr[nameLength] - 48) * 100 + (linePtr[nameLength + 1] - 48) * 10 + linePtr[nameLength + 2] - 48,
									_ => (linePtr[nameLength] - 48) * 10 + (linePtr[nameLength + 1] - 48)
								});
								break;
							}
							case IntParseFlag.HasDot: {
								temperature = (idx - nameLength) switch {
									3 => (linePtr[nameLength] - 48) * 100 + (linePtr[nameLength + 1] - 48) * 10 + linePtr[nameLength + 2] - 48,
									_ => (linePtr[nameLength] - 48) * 10 + (linePtr[nameLength + 1] - 48)
								};
								break;
							}
							case IntParseFlag.Signed: {
								temperature = -10 * (idx - nameLength) switch {
									2 => (linePtr[nameLength] - 48) * 10 + linePtr[nameLength + 1] - 48,
									_ => linePtr[nameLength] - 48
								};
								break;
							}
							case IntParseFlag.None: {
								temperature = 10 * (idx - nameLength) switch {
									2 => (linePtr[nameLength] - 48) * 10 + linePtr[nameLength + 1] - 48,
									_ => linePtr[nameLength] - 48
								};
								break;
							}
						}

						if (dic.TryGetValue(key, out var container)) {
							container.Update(temperature);
						} else {
							dic.Add(key, new TemperatureContainer(Encoding.UTF8.GetString(linePtr.Slice(0, nameLength)), temperature));
						}

						idx = 0;
						flag = IntParseFlag.None;
						break;
					}
					default: {
						linePtr[idx++] = value;
						break;
					}
				}
			}

			return Task.FromResult(new FirstAndLastPart(firstLineEndIdx, lastLineStartIdx, readBytes));
		}

		[Flags]
		private enum
			IntParseFlag : byte {
			None = 0,
			Signed = 1,
			HasDot = 2
		}
	}

	internal struct FirstAndLastPart {
		public int FirstPartCount { get; }

		public int LastPartIndex { get; }

		public int LastPartCount { get; }

		public FirstAndLastPart(int? firstPartCount, int? lastPartIndex, int count)
			: this() {
			if (firstPartCount.HasValue) {
				FirstPartCount = firstPartCount.Value;
			}

			if (lastPartIndex.HasValue) {
				LastPartIndex = lastPartIndex.Value;
				LastPartCount = count - lastPartIndex.Value;
			}
		}
	}

	internal class TemperatureContainer {
		private const double Divider = 10.0;

		#region

		private int _min;
		private int _sum;
		private int _max;
		private int _count;

		#endregion

		public TemperatureContainer(string name, int temperature) {
			Name = name;
			_sum = temperature;
			_min = temperature;
			_max = temperature;
			_count = 1;
		}

		public string Name { get; }

		public double Min => _min / Divider;

		public double Average => _sum / Divider / _count;

		public double Max => _max / Divider;

		public void Update(int temperature) {
			_sum += temperature;
			_count++;

			if (temperature < _min) {
				_min = temperature;
			} else if (temperature > _max) {
				_max = temperature;
			}
		}
	}
}