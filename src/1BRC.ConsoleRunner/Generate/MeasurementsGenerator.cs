using System;
using System.IO;
using System.Threading.Tasks;

namespace _1BRC.Framework.Console.Generate {
	internal class MeasurementsGenerator {
		private const int MaxNameCount = 10000;

		public static void CreateFile(string filePath, int totalRowCount) {
			using (var writer = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 44800, FileOptions.SequentialScan)) {
				var names = CreateRandomNames();
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
				
				#if DEBUG
				const int maxParallel = 1;
				#else
				var maxParallel = Environment.ProcessorCount;
				#endif
				var sliceOfRowCount = (int)Math.Ceiling(totalRowCount / (double)maxParallel);

				Parallel.For(0, maxParallel, new ParallelOptions {
					MaxDegreeOfParallelism = maxParallel
				}, processIdx => {
					const int blockSize = 102400;
					var block = new byte[blockSize];
					var blockIndex = 0;

					var rowCount = sliceOfRowCount;
					var rowCountAfterGeneration = rowCount * (processIdx + 1);
					var diff = rowCountAfterGeneration - totalRowCount;
					if (diff > 0) {
						rowCount -= diff;
					}

					var random = ThreadSafeRandom.Instance;
					var localWriter = writer;
                    
					for (var i = 0; i < rowCount; i++) {
						// Pick name
						var name = names[random.Next(MaxNameCount)];

						// Write name to block
						var nameLength = name.Length;
						Array.Copy(name, 0, block, blockIndex, nameLength);
						blockIndex += nameLength;

						const byte lineSeparator = 59; // ;
						block[blockIndex] = lineSeparator;
						blockIndex++;

						// Create temperature
						// The random value range needs to be 1 to 1999 because the result will be divided by 10 and afterwards 100 is subtracted
						// which will result in the new value range from -99.9 to 99.9
						const byte zeroByte = 48; // 0
						var fractionValue = numberBytesForTemperature[random.Next(10)];
						var singleDigit = numberBytesForTemperature[random.Next(10)];
						var doubleDigit = numberBytesForTemperature[random.Next(10)];
						var useDoubleDigit = doubleDigit != zeroByte;

						// Write temperature to block
						if ((singleDigit != zeroByte || useDoubleDigit) && random.Next(2) == 0) {
							const byte signedSymbol = 45; // -
							block[blockIndex] = signedSymbol;
							blockIndex++;
						}

						if (useDoubleDigit) {
							block[blockIndex] = doubleDigit;
							blockIndex++;
						}

						block[blockIndex] = singleDigit;
						blockIndex++;

						if (fractionValue != zeroByte) {
							const byte temperatureSeparator = 46; // .
							block[blockIndex] = temperatureSeparator;
							blockIndex++;
							block[blockIndex] = fractionValue;
							blockIndex++;
						}

						// Write new line to block
						const byte newLine = 10; // \n
						block[blockIndex] = newLine;
						blockIndex++;

						// Write block to stream
						const int maxLineLength = 106;
						if (blockSize - blockIndex >= maxLineLength) {
							continue;
						}

						localWriter.Write(block, 0, blockIndex);
						blockIndex = 0;
					}

					if (blockIndex != 0) {
						localWriter.Write(block, 0, blockIndex);
					}
				});
			}
		}

		private static byte[][] CreateRandomNames() {
			const byte arraySize = 253; // 3 bytes will be ignored
			const byte firstIgnoreValue = 10; // \n
			const byte secondIgnoreValue = 13; // \r
			const byte thirdIgnoreValue = 59; // ;

			var validNameBytes = new byte[arraySize];
			for (int i = 0, j = 0; i <= byte.MaxValue && j < arraySize; i++) {
				var nextByte = (byte)i;
				if (nextByte is firstIgnoreValue or secondIgnoreValue or thirdIgnoreValue) {
					continue;
				}

				validNameBytes[j] = nextByte;
				j++;
			}

			var names = new byte[MaxNameCount][];

			for (var i = 0; i < MaxNameCount; i++) {
				const int randomMinIncluded = 7; // Name needs at least 1 character
				//const int randomMaxExcluded = 100 + 1; // Name can have up to 100 characters

				var nameLength = randomMinIncluded; // ThreadSafeRandom.Instance.Next(randomMinIncluded, randomMaxExcluded);
				var bytes = new byte[nameLength];

				for (var j = 0; j < nameLength; j++) {
					bytes[j] = validNameBytes[ThreadSafeRandom.Instance.Next(arraySize)];
				}

				names[i] = bytes;
			}

			return names;
		}
	}
}