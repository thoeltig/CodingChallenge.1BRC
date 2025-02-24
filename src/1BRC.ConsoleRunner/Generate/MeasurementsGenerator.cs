using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace _1BRC.ConsoleRunner.Generate {
	internal class MeasurementsGenerator {
		private const int MaxNameCount = 10000;

		public static void CreateFile(string filePath, int totalRowCount) {
			var names = CreateRandomNames();

			using (var writer = new StreamWriter(filePath, false, Encoding.UTF8)) {
				writer.NewLine = "\n";

				for (var i = 0; i < totalRowCount; i++) {
					var line = GetLine(names);
					writer.WriteLine(line);
				}
			}
		}

		private static string GetLine(IReadOnlyList<string> names) {
			const string separator = ";";

			var name = names[ThreadSafeRandom.Instance.Next(MaxNameCount)];
			var temperature = GetRandomTemperature();
			return string.Concat(name, separator, temperature);
		}

		private static string GetRandomTemperature() {
			// The random value range needs to be 1 to 1999 because the result will be divided by 10 and afterwards 100 is subtracted
			// which will result in the new value range from -99.9 to 99.9
			const int randomMinIncluded = 1;
			const int randomMaxExcluded = 1999 + 1;
			const double divider = 10;
			const double subtractedValue = 100;
			const int digits = 1;

			var randomValue = ThreadSafeRandom.Instance.Next(randomMinIncluded, randomMaxExcluded);
			var temperature = Math.Round(randomValue / divider - subtractedValue, digits);
			return temperature.ToString(CultureInfo.InvariantCulture);
		}

		private static IReadOnlyList<string> CreateRandomNames() {
			const byte arraySize = 253; // 3 bytes will be ignored
			const byte firstIgnoreValue = 10; // \n
			const byte secondIgnoreValue = 13; // \r
			const byte thirdIgnoreValue = 59; // ;

			var validNameChars = new char[arraySize];
			for (int i = 0, j = 0; i <= byte.MaxValue && j < arraySize; i++) {
				var nextByte = (byte)i;
				if (nextByte is not (firstIgnoreValue or secondIgnoreValue or thirdIgnoreValue)) {
					validNameChars[j] = (char)nextByte;
					j++;
				}
			}

			var names = new string[MaxNameCount];

			for (var i = 0; i < MaxNameCount; i++) {
				const int randomMinIncluded = 5; // Name needs at least 1 character
				const int randomMaxExcluded = 9 + 1; // Name can have up to 100 characters

				var nameLength = ThreadSafeRandom.Instance.Next(randomMinIncluded, randomMaxExcluded);
				var chars = new char[nameLength];

				for (var j = 0; j < nameLength; j++) {
					chars[j] = validNameChars[ThreadSafeRandom.Instance.Next(arraySize)];
				}

				names[i] = new string(chars);
			}

			return names;
		}
	}
}