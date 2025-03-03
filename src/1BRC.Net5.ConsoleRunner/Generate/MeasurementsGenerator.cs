using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using _1BRC.Framework.Console.Generate;

namespace _1BRC.Net5.ConsoleRunner.Generate {
	internal class MeasurementsGenerator {
		public static void GenerateFile(string filePath, int totalRowCount) {
			using (var writer = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 262144, FileOptions.SequentialScan)) {
				#if DEBUG
			const int maxParallel = 1;
				#else
				var maxParallel = Environment.ProcessorCount;
				#endif

				const int blockSize = 16777216;
				var tasks = new Task[maxParallel];
				var block = new byte[blockSize];
				var blockAsSpan = block.AsSpan();

				ReadOnlySpan<byte[]> names = WeatherStation.Names.AsSpan();
				var maxNameCount = names.Length;
				ReadOnlySpan<byte> numberBytesForTemperature = new byte[] {
					48, // 0
					49, // 1
					50, // 2
					51, // 3
					52, // 4
					53, // 5
					54, // 6
					55, // 7
					56, // 8
					57 // 9
				}.AsSpan();
				var numberBytesForTemperatureCount = numberBytesForTemperature.Length;
				var linesToCreate = totalRowCount;
				var nameIndex = 0;
				var numberIndex = 0;
				var blockIndex = 0;

				while (linesToCreate > 0) {
					blockIndex = 0;
					for (var j = 0; j < maxParallel; j++) {
						tasks[j] = GenerateLinesAsync(blockAsSpan, GetSlice, names, GetNameIndex, numberBytesForTemperature, GetNumber);
					}

					Task.WaitAll(tasks);

					writer.Write(block, 0, blockIndex);
				}

				return;

				int GetSlice(int lineLength) {
					var idx = Interlocked.Decrement(ref linesToCreate);
					if (idx < 0) {
						return -1;
					}

					idx = blockIndex;
					var newIdx = Interlocked.Add(ref blockIndex, lineLength);
					if (blockSize - newIdx >= 0) {
						return idx;
					}

					Interlocked.Exchange(ref blockIndex, idx);
					Interlocked.Increment(ref linesToCreate);
					return -1;
				}

				int GetNameIndex() {
					if (++nameIndex < maxNameCount) {
						return nameIndex;
					}

					nameIndex = 0;
					return 0;
				}

				int GetNumber() {
					if (++numberIndex < numberBytesForTemperatureCount) {
						return numberIndex;
					}

					numberIndex = 0;
					return 0;
				}
			}
		}

		private static Task GenerateLinesAsync(Span<byte> block, Func<int, int> getBlockIndexFunc, ReadOnlySpan<byte[]> names, Func<int> getNameIndexFunc, ReadOnlySpan<byte> numbers, Func<int> getNumberIndexFunc) {
			const byte zeroByte = 48; // 0
			const byte lineSeparator = 59; // ;
			const byte signedSymbol = 45; // -
			const byte temperatureSeparator = 46; // .
			const byte newLine = 10; // \n

			for (;;) {
				// plan line
				ReadOnlySpan<byte> name = names[getNameIndexFunc()].AsSpan();
				var nameLength = name.Length;
				var fractionValue = numbers[getNumberIndexFunc()];
				var singleDigit = numbers[getNumberIndexFunc()];
				var doubleDigit = numbers[getNumberIndexFunc()];
				var useDoubleDigit = doubleDigit != zeroByte;
				var useSignedSymbol = (singleDigit != zeroByte || useDoubleDigit) && nameLength % 2 == 0;
				var useFraction = fractionValue != zeroByte;

				var lineLength = nameLength + (useSignedSymbol ? 2 : 1) + (useDoubleDigit ? 2 : 1) + (useFraction ? 3 : 2);
				var blockIdx = getBlockIndexFunc(lineLength);
				if (blockIdx == -1) {
					break;
				}

				var slice = block.Slice(blockIdx, lineLength);
				name.CopyTo(slice);
				var idx = nameLength;

				slice[idx++] = lineSeparator;

				if (useSignedSymbol) {
					slice[idx++] = signedSymbol;
				}

				if (useDoubleDigit) {
					slice[idx++] = doubleDigit;
				}

				slice[idx++] = singleDigit;

				if (useFraction) {
					slice[idx++] = temperatureSeparator;
					slice[idx++] = fractionValue;
				}

				slice[idx] = newLine;
			}

			return Task.CompletedTask;
		}
	}
}