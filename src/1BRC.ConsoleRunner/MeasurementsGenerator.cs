using System;
using System.IO;
using System.Text;

namespace _1BRC.ConsoleRunner {
	internal class MeasurementsGenerator {
		private const int MaxNameCount = 10000;
		private static readonly Random _random = new Random();

		public static void CreateFile(string filePath, int rowCount) {
			var names = CreateRandomNames();

			using (var writer = new StreamWriter(filePath, false, Encoding.UTF8)) {
				writer.NewLine = "\n";

				for (var i = 0; i < rowCount; i++) {
					var line = GetLine(names);
					writer.WriteLine(line);
				}
			}
		}

		private static string GetLine(string[] names) {
			const string separator = ";";

			var name = GetRandomName(names);
			var temperature = GetRandomTemperature();
			return string.Concat(name, separator, temperature);
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

		private static string GetRandomName(string[] names) => names[_random.Next(MaxNameCount)];

		private static string[] CreateRandomNames() {
			var validNameBytes = GetValidBytesForName();
			var names = new string[MaxNameCount];
			for (var i = 0; i < MaxNameCount; i++) {
				var nameBytes = GetRandomNameBytes(validNameBytes);
				names[i] = Encoding.UTF8.GetString(nameBytes);
			}

			return names;
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