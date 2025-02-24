using System;
using System.Diagnostics;
using System.IO;
using _1BRC.ConsoleRunner.Generate;

namespace _1BRC.ConsoleRunner {
	internal class Program {
		private const int RowCount = 10000;
		private const string FileName = "measurements.txt";

		private static void Main(string[] args) {
			Console.WriteLine($"Generating {RowCount:N0} rows");
			Console.WriteLine();
			File.Delete(FileName);

			var sw = new Stopwatch();
			sw.Restart();
			MeasurementsGeneratorV1.CreateFile(FileName, RowCount);
			sw.Stop();

			Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
		}
	}
}