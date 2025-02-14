using System;
using System.IO;
using System.Text;

namespace _1BRC.ConsoleRunner {
	internal class MeasurementsGenerator {
		private static readonly Random _random = new Random();

		public static void CreateFile(string filePath, int rowCount, Action<int> progressCallback) {
			const int maxNameCount = 10000;
			const string newLine = "\n";
			const string separator = ";";

			var validNameBytes = GetValidBytesForName();
			var names = new string[maxNameCount];
			for (var i = 0; i < maxNameCount; i++) {
				var nameBytes = GetRandomNameBytes(validNameBytes);
				names[i] = Encoding.UTF8.GetString(nameBytes);
			}

			using (var stream = new FileStream(filePath, FileMode.OpenOrCreate)) {
				using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
					writer.NewLine = newLine;

					for (var i = 0; i < rowCount; i++) {
						var name = names[_random.Next(maxNameCount)];
						var temperature = GetRandomTemperature();
						writer.WriteLine(string.Concat(name, separator, temperature));
						progressCallback(i);
					}
				}
			}
		}

		private static double GetRandomTemperature() {
			// The random value range needs to be 1 to 1999 because the result will be divided by 10 and afterwards 100 is subtracted
			// which will result in the new value range from -99.9 to 99.9
			const int randomMinIncluded = 1;
			const int randomMaxExcluded = 1999 + 1;
			const double divider = 10;
			const double subtractedValue = 100;
			const int digits = 1;

			return Math.Round(_random.Next(randomMinIncluded, randomMaxExcluded) / divider - subtractedValue, digits);
		}

		private static byte[] GetRandomNameBytes(byte[] validNameBytes) {
			const int randomMinIncluded = 1; // Name needs at least 1 character
			const int randomMaxExcluded = 100 + 1; // Name can have up to 100 characters

			var nameLength = _random.Next(randomMinIncluded, randomMaxExcluded);
			var nameBytes = new byte[nameLength];
			var idxMax = validNameBytes.Length;

			for (var i = 0; i < nameLength; i++) {
				var idx = _random.Next(idxMax);
				nameBytes[i] = validNameBytes[idx];
			}

			return nameBytes;
		}

		private static byte[] GetValidBytesForName() {
			const int randomMaxExcluded = byte.MaxValue + 1;
			const int arraySize = 253; // 3 bytes will be ignored
			const byte firstIgnoreValue = 10; // \n
			const byte secondIgnoreValue = 13; // \r
			const byte thirdIgnoreValue = 59; // ;

			var array = new byte[arraySize];
			for (var i = 0; i < arraySize; i++) {
				var nextByte = (byte)_random.Next(randomMaxExcluded);
				if (nextByte == firstIgnoreValue || nextByte == secondIgnoreValue || nextByte == thirdIgnoreValue) {
					i--;
				} else {
					array[i] = nextByte;
				}
			}

			return array;
		}
	}
}