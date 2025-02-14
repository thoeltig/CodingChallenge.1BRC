using System;
using System.Diagnostics;

namespace _1BRC.ConsoleRunner {
	internal class Program {
		private const string FileName = "measurements.txt";

		private static void Main(string[] args) {
			var sw = new Stopwatch();
			CreateFile(sw);
			Console.ReadKey();
		}

		private static void CreateFile(Stopwatch sw) {
			const int rowSize = 10000000;

			Console.WriteLine($"Generating {rowSize:N0} rows");

			sw.Start();
			MeasurementsGenerator.CreateFile(FileName, rowSize);
			sw.Stop();

			Console.WriteLine();
			Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
		}
	}
}