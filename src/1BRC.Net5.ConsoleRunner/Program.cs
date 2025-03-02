using System.Diagnostics;
using System.IO;
using _1BRC.Net5.ConsoleRunner.Generate;

namespace _1BRC.Net5.ConsoleRunner {
	internal class Program {
		#if DEBUG
		private const int RowCount = 10000000;
		#else
		private const int RowCount = 1000000000;
		#endif
		private const string FileName = "measurements.txt";

		private static void Main(string[] args) {
			System.Console.WriteLine($"Generating {RowCount:N0} rows");
			System.Console.WriteLine();
			File.Delete(FileName);

			var sw = new Stopwatch();
			sw.Restart();
			MeasurementsGenerator.GenerateFile(FileName, RowCount);
			sw.Stop();

			System.Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
			System.Console.ReadKey();
		}
	}
}