using System;
using System.Diagnostics;

namespace _1BRC.ConsoleRunner {
	internal class Program {
		private const int RowSize = 1000000;
		private const int UpdateStepSize = 1000;
		private const string FileName = "measurements.txt";

		private static void Main(string[] args) {
			var sw = new Stopwatch();
			CreateFile(sw);
			Console.ReadKey();
		}

		private static void CreateFile(Stopwatch sw) {
			var firstLine = $"Generating {RowSize:N0} rows: ";
			var offset = firstLine.Length;
			Console.WriteLine(firstLine);

			sw.Start();
			MeasurementsGenerator.CreateFile(FileName, RowSize, i => UpdateProgress(i, offset));
			sw.Stop();

			WritePercentageToConsole(offset, 100.00);
			Console.WriteLine();
			Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
		}

		private static void UpdateProgress(int currentValue, int cursorOffset) {
			if (currentValue % UpdateStepSize != 0) {
				return;
			}

			var percentage = Math.Round((currentValue + 1.0) / RowSize * 100, 2);
			WritePercentageToConsole(cursorOffset, percentage);
		}

		private static void WritePercentageToConsole(int cursorOffset, double percentage) {
			Console.SetCursorPosition(cursorOffset, 0);
			Console.Write($"{percentage:N} %");
		}
	}
}