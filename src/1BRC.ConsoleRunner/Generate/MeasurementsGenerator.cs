using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace _1BRC.Framework.ConsoleRunner.Generate {
	internal class MeasurementsGenerator {
		private const int MaxNameCount = 10000;

		public static void CreateFile(string filePath, int totalRowCount) {
			var names = CreateRandomNames();

			using (var writer = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 44800, FileOptions.SequentialScan)) {
				const byte separator = 59; // ;
				const byte newLine = 10; // \n
				const int maxLineLength = 106;
				const int blockSize = 102400;
				var block = new byte[blockSize];
				var blockIndex = 0;

				for (var i = 0; i < totalRowCount; i++) {
					var name = names[ThreadSafeRandom.Instance.Next(MaxNameCount)];
					var temperature = GetRandomTemperature();

					var nameLength = name.Length;
					var temperatureLength = temperature.Length;

					Array.Copy(name, 0, block, blockIndex, nameLength);
					blockIndex += nameLength;

					block[blockIndex] = separator;
					blockIndex++;

					Array.Copy(temperature, 0, block, blockIndex, temperatureLength);
					blockIndex += temperatureLength;

					block[blockIndex] = newLine;
					blockIndex++;

					if (blockSize - blockIndex < maxLineLength) {
						writer.Write(block, 0, blockIndex);
						blockIndex = 0;
					}
				}
			}
		}

		private static byte[] GetRandomTemperature() {
			// The random value range needs to be 1 to 1999 because the result will be divided by 10 and afterwards 100 is subtracted
			// which will result in the new value range from -99.9 to 99.9
			const int randomMinIncluded = 1;
			const int randomMaxExcluded = 1999 + 1;
			const double divider = 10;
			const double subtractedValue = 100;
			const int digits = 1;

			var randomValue = ThreadSafeRandom.Instance.Next(randomMinIncluded, randomMaxExcluded);
			var temperature = Math.Round(randomValue / divider - subtractedValue, digits);
			return Encoding.UTF8.GetBytes(temperature.ToString(CultureInfo.InvariantCulture));
		}

		private static byte[][] CreateRandomNames() {
			const byte arraySize = 253; // 3 bytes will be ignored
			const byte firstIgnoreValue = 10; // \n
			const byte secondIgnoreValue = 13; // \r
			const byte thirdIgnoreValue = 59; // ;

			var validNameBytes = new byte[arraySize];
			for (int i = 0, j = 0; i <= byte.MaxValue && j < arraySize; i++) {
				var nextByte = (byte)i;
				if (nextByte is not (firstIgnoreValue or secondIgnoreValue or thirdIgnoreValue)) {
					validNameBytes[j] = nextByte;
					j++;
				}
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