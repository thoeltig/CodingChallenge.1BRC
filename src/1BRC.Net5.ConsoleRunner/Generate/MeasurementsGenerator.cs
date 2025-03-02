using System;
using System.IO;
using System.Threading.Tasks;
using _1BRC.Framework.Console.Generate;

namespace _1BRC.Net5.ConsoleRunner.Generate {
	internal class MeasurementsGenerator {
		public static void GenerateFile(string filePath, int totalRowCount) {
			using (var writer = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 44800, FileOptions.SequentialScan)) {
				#if DEBUG
			const int maxParallel = 1;
				#else
				var maxParallel = Environment.ProcessorCount;
				#endif

				const int blockSize = 16777216;
				var tasks = new Task[maxParallel];
				var block = new byte[blockSize];
				var sliceOfBlock = (int)Math.Ceiling(blockSize / (double)maxParallel);

				var slices = new byte[maxParallel][];
				for (var i = 0; i < maxParallel; i++) {
					slices[i] = new byte[sliceOfBlock];
				}

				var names = WeatherStation.Names;
				var maxNameCount = names.Length;
				var numberBytesForTemperature = new byte[] {
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
				};
				var numberBytesForTemperatureCount = numberBytesForTemperature.Length;
				var linesToCreate = totalRowCount;
				var nameIndex = 0;
				var numberIndex = 0;

				while (linesToCreate > 0) {
					for (var j = 0; j < maxParallel; j++) {
						tasks[j] = GenerateLinesAsync(slices[j].AsSpan(), CanCreateLine, GetName, GetNumber);
					}

					Task.WaitAll(tasks);

					var blockIndex = 0;
					for (var j = 0; j < maxParallel; j++) {
						var length = ((Task<int>)tasks[j]).Result;
						Buffer.BlockCopy(slices[j], 0, block, blockIndex, length);
						blockIndex += length;
					}

					_ = writer.WriteAsync(block, 0, blockIndex).ConfigureAwait(false);
				}

				return;

				bool CanCreateLine() => --linesToCreate > 0;

				byte[] GetName() {
					var idx = ++nameIndex;
					if (idx < maxNameCount) {
						return names[idx];
					}

					nameIndex = 0;
					return names[0];
				}

				byte GetNumber() {
					var idx = ++numberIndex;
					if (idx < numberBytesForTemperatureCount) {
						return numberBytesForTemperature[idx];
					}

					numberIndex = 0;
					return numberBytesForTemperature[0];
				}
			}
		}

		private static Task<int> GenerateLinesAsync(Span<byte> slice, Func<bool> canCreatedAnotherLine, Func<byte[]> getName, Func<byte> getNumber) {
			const int maxLineLength = 106;
			var idx = 0;
			var capacity = slice.Length;

			while (canCreatedAnotherLine() && capacity - idx >= maxLineLength) {
				// Write name to block
				var name = getName();
				for (var i = 0; i < name.Length; i++, idx++) {
					slice[idx] = name[i];
				}

				const byte lineSeparator = 59; // ;
				slice[idx++] = lineSeparator;

				// Create temperature
				// The random value range needs to be 1 to 1999 because the result will be divided by 10 and afterwards 100 is subtracted
				// which will result in the new value range from -99.9 to 99.9
				const byte zeroByte = 48; // 0
				var fractionValue = getNumber();
				var singleDigit = getNumber();
				var doubleDigit = getNumber();
				var useDoubleDigit = doubleDigit != zeroByte;

				// Write temperature to block
				if ((singleDigit != zeroByte || useDoubleDigit) && idx % 3 == 0) {
					const byte signedSymbol = 45; // -
					slice[idx++] = signedSymbol;
				}

				if (useDoubleDigit) {
					slice[idx++] = doubleDigit;
				}

				slice[idx++] = singleDigit;

				if (fractionValue != zeroByte) {
					const byte temperatureSeparator = 46; // .
					slice[idx++] = temperatureSeparator;
					slice[idx++] = fractionValue;
				}

				// Write new line to block
				const byte newLine = 10; // \n
				slice[idx++] = newLine;
			}

			return Task.FromResult(idx);
		}
	}
}