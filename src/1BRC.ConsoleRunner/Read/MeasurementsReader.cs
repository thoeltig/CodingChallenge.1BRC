using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace _1BRC.Framework.Console.Read {
	internal static class MeasurementsReader {
		public static IReadOnlyDictionary<string, TemperatureContainer> ReadFile(string measurementsFilePath) {
			var dic = new Dictionary<string, TemperatureContainer>();

			using (var reader = new StreamReader(measurementsFilePath, Encoding.UTF8)) {
				while (reader.ReadLine() is { } line) {
					var parts = line.Split(new[] {
						';'
					}, StringSplitOptions.RemoveEmptyEntries);

					var key = parts[0];
					var temperature = double.TryParse(parts[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var temp) ? temp : 0.0;
					if (dic.TryGetValue(key, out var container)) {
						container.Update(temperature);
					} else {
						dic.Add(key, new TemperatureContainer(temperature));
					}
				}
			}

			return dic;
		}
	}
}