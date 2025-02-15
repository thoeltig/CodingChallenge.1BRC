using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace _1BRC.ConsoleRunner {
	internal class MeasurementsGenerator {
		private const int MaxNameCount = 10000;
		private static readonly Random _random = new Random();

		public static void CreateFile(string filePath, int rowCount) {
			var names = CreateRandomNames();

			using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write)) {
				stream.SetLength(0);

				const int rowBuffer = 1024;
				var lines = new byte[rowBuffer][];
				for (var i = 0; i < rowCount;) {
					for (var j = 0; j < rowBuffer && i < rowCount; j++, i++) {
						lines[j] = GetLine(names);
					}

					var array = lines.SelectMany(x => x).ToArray();
					stream.Write(array, 0, array.Length);
				}
			}
		}

		private static byte[] GetLine(byte[][] names) {
			const byte separator = 59; // ;
			const byte newLine = 10; // \n

			var name = GetRandomName(names);
			var temperature = GetRandomTemperature();
			var result = new byte[name.Length + temperature.Length + 2];
			Array.Copy(name, 0, result, 0, name.Length);
			result[name.Length] = separator;
			Array.Copy(temperature, 0, result, name.Length + 1, temperature.Length);
			result[result.Length - 1] = newLine;
			return result;
		}

		private static byte[] GetRandomTemperature() {
			// The random value range needs to be 1 to 1999 because the result will be divided by 10 and afterwards 100 is subtracted
			// which will result in the new value range from -99.9 to 99.9
			const int randomMinIncluded = 1;
			const int randomMaxExcluded = 1999 + 1;
			const double divider = 10;
			const double subtractedValue = 100;
			const int digits = 1;

			var randomValue = _random.Next(randomMinIncluded, randomMaxExcluded);
			var temperature = Math.Round(randomValue / divider - subtractedValue, digits);
			return Encoding.UTF8.GetBytes(temperature.ToString(CultureInfo.InvariantCulture));
		}

		private static byte[] GetRandomName(byte[][] names) => names[_random.Next(MaxNameCount)];

		private static byte[][] CreateRandomNames() {
			var validNameBytes = GetValidBytesForName();
			var names = new byte[MaxNameCount][];
			for (var i = 0; i < MaxNameCount; i++) {
				names[i] = GetRandomNameBytes(validNameBytes);
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