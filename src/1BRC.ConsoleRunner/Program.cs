using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using _1BRC.Framework.Console.Generate;
using _1BRC.Framework.Console.Read;

namespace _1BRC.Framework.Console {
	internal class Program {
		#if DEBUG
		private const int RowCount = 10000000;
		#else
		private const int RowCount = 1000000000;
		#endif
		private const string MeasurementsFileName = "measurements.txt";
		private const string ResultFileName = "result.txt";

		private static void Main(string[] args) {
			var sw = new Stopwatch();
			if (File.Exists(MeasurementsFileName) == false) {
				System.Console.WriteLine($"Generating {RowCount:N0} rows");

				sw.Start();
				MeasurementsGenerator.GenerateFile(MeasurementsFileName, RowCount);
				sw.Stop();

				System.Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
			}

			IReadOnlyDictionary<string, TemperatureContainer> result = null;
			for (var i = 0; i < 6; i++) {
				System.Console.WriteLine();
				System.Console.WriteLine("Reading file");

				sw.Reset();
				sw.Start();
				result = MeasurementsReader.ReadFile(MeasurementsFileName);
				sw.Stop();

				System.Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
			}

			if (result != null) {
				using (var writer = new StreamWriter(ResultFileName, false)) {
					const string separator = " | ";

					writer.WriteLine("name | min | average | max");
					writer.WriteLine();

					foreach (var keyVal in result) {
						writer.WriteLine(string.Concat(keyVal.Key, separator, keyVal.Value.Min, separator, keyVal.Value.Average, separator, keyVal.Value.Max));
					}
				}

				System.Console.WriteLine($"Result: {ResultFileName}");
			}

			System.Console.ReadKey();
		}
	}
}