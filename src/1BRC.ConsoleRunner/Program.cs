using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace _1BRC.ConsoleRunner {
	internal class Program {
		private const int RowSize = 10000000;
		private const string FileName = "measurements.txt";

		private static void Main(string[] args) {
			const int count = 5;
			var times = new TimeSpan[count];
			var sw = new Stopwatch();

			Console.WriteLine($"Generating {RowSize:N0} rows");
            Console.WriteLine();

			foreach (var fileBufferSize in new[] { 4096, 8192, 16384, 32768, 65536 }) {
				foreach (var rowBufferSize in new[] { 500, 1000, 2000}) {
                    File.Delete(FileName);

			        Console.WriteLine($"File writer buffer size {fileBufferSize} and row buffer size {rowBufferSize}");

					for (var i = 0; i < count; i++) {
						times[i] = CreateFile(sw, rowBufferSize, fileBufferSize);
					}

					var avg = new TimeSpan(times.Sum(x => x.Ticks) / count);
					Console.WriteLine($"Avg: {avg:mm':'ss':'fff}");
                    Console.WriteLine();
				}	
			}

			Console.ReadKey();
		}

		private static TimeSpan CreateFile(Stopwatch sw, int rowBuffer, int fileBuffer) {
			sw.Restart();
			MeasurementsGenerator.CreateFile(FileName, RowSize, rowBuffer, fileBuffer);
			sw.Stop();

			Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
			return sw.Elapsed;
		}
	}
}