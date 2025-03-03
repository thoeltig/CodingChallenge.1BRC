using System.Collections.Generic;
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
				var foundDot = false;

				int readByte;
				while ((readByte = stream.ReadByte()) != -1) {
					var value = (byte)readByte;
					line[idx++] = value;

					switch (value) {
						case lineSeparator: {
							nameLength = idx;
							break;
						}
						case temperatureSeparator: {
							foundDot = true;
							idx--;
							break;
						}
						case newLine: {
							var key = Encoding.UTF8.GetString(line, 0, nameLength - 1);
							var numberIdx = nameLength;
							var numberLength = idx - numberIdx - 1;
							if (foundDot == false) {
								line[idx - 1] = zeroByte;
								numberLength = idx - numberIdx;
							}

							var temperatureString = Encoding.UTF8.GetString(line, numberIdx, numberLength);
							var temperature = int.TryParse(temperatureString, out var temp) ? temp : 0;
							if (dic.TryGetValue(key, out var container)) {
								container.Update(temperature);
							} else {
								dic.Add(key, new TemperatureContainer(temperature));
							}

							idx = 0;
							nameLength = 0;
							foundDot = false;
							break;
						}
					}
				}
			}

			return dic;
		}
	}

	internal class TemperatureContainer {
		private const double Divider = 10.0;
		private int _count;
		private int _min;
		private int _max;
		private int _sum;

		public double Min => _min / Divider;

		public double Average => _sum / Divider / _count;

		public double Max => _max / Divider;

		public TemperatureContainer(int temperature) {
			_sum = temperature;
			_min = temperature;
			_max = temperature;
			_count = 1;
		}

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