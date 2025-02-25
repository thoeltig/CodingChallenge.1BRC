using System.Diagnostics;
using System.IO;
using _1BRC.Framework.Console.Generate;

namespace _1BRC.Framework.Console {
	internal class Program {
		private const int RowCount = 1000000000;
		private const string FileName = "measurements.txt";

		private static void Main(string[] args) {
			System.Console.WriteLine($"Generating {RowCount:N0} rows");
			System.Console.WriteLine();
			File.Delete(FileName);

			var sw = new Stopwatch();
			sw.Restart();
			MeasurementsGenerator.CreateFile(FileName, RowCount);
			sw.Stop();

			System.Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
			System.Console.ReadKey();
		}
	}
}