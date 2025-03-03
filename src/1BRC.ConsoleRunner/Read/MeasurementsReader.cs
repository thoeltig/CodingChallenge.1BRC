using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace _1BRC.Framework.Console.Read {
	internal static class MeasurementsReader {
		public static IReadOnlyDictionary<string, TemperatureContainer> ReadFile(string measurementsFilePath) {
			var dic = new Dictionary<string, TemperatureContainer>();

			using (var stream = new FileStream(measurementsFilePath, FileMode.Open, FileAccess.Read, FileShare.None, 262144, FileOptions.SequentialScan | FileOptions.WriteThrough)) {
				const byte lineSeparator = 59; // ;
				const byte zeroByte = 48; // 0
				const byte signedSymbol = 45; // -
				const byte temperatureSeparator = 46; // .
				const byte newLine = 10; // \n

				var line = new byte[106];
				var idx = 0;
				var nameLength = 0;

				int readByte;
				while ((readByte = stream.ReadByte()) != -1) {
					var value = (byte)readByte;
					line[idx++] = value;

					switch (value) {
						case lineSeparator: {
							nameLength = idx;
							break;
						}
						case newLine: {
							var key = Encoding.UTF8.GetString(line, 0, nameLength - 1);
							var numberIdx = nameLength;
							var temperatureString = Encoding.UTF8.GetString(line, numberIdx, idx - numberIdx -1);
							var temperature = double.TryParse(temperatureString, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var temp) ? temp : 0.0;
							if (dic.TryGetValue(key, out var container)) {
								container.Update(temperature);
							} else {
								dic.Add(key, new TemperatureContainer(temperature));
							}

							idx = 0;
							nameLength = 0;
							break;
						}
					}
				}
			}

			return dic;
		}
	}

	internal class TemperatureContainer {
		public double Min { get; private set; }

		public double Average { get; private set; }

		public double Max { get; private set; }

		public TemperatureContainer(double temperature) {
			Average = temperature;
			Min = temperature;
			Max = temperature;
		}

		public void Update(double temperature) {
			Average = (Average + temperature) / 2.0;

			if (temperature < Min) {
				Min = temperature;
			} else if (temperature > Max) {
				Max = temperature;
			}
		}
	}
}