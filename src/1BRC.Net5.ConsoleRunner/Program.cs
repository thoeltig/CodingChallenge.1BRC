using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using _1BRC.Net9.ConsoleRunner.Generate;
using _1BRC.Net9.ConsoleRunner.Read;

namespace _1BRC.Net9.ConsoleRunner {
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

			IReadOnlyCollection<TemperatureContainer> result = null;
			for (var i = 0; i < 4; i++) {
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

					foreach (var container in result) {
						writer.WriteLine(string.Concat(container.Name, separator, container.Min, separator, container.Average, separator, container.Max));
					}
				}

				System.Console.WriteLine($"Result: {ResultFileName}");
			}

			System.Console.ReadKey();
		}
	}
}