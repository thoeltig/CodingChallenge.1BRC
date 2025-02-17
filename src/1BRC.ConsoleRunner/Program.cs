using System;
using System.Diagnostics;
//using System.IO;
//using System.Linq;

namespace _1BRC.ConsoleRunner {
	internal class Program {
		//private const int RowSize = 10000000;
		//private const int RowBufferSize = 4000;
		//private const int FileBufferSize = 32768;
		//private const string FileName = "measurements.txt";

		private static void Main(string[] args) {
			Console.WriteLine("File write test started");
			var sw = new Stopwatch();
            sw.Start();
			FileReadAndWrite.ExecuteTest(Console.WriteLine);
            sw.Stop();
			Console.WriteLine($"File write test done! Took {sw.Elapsed:mm':'ss':'fff} to finish!");
            
			//const int count = 5;
			//var times = new TimeSpan[count];
			//Console.WriteLine($"Generating {RowSize:N0} rows");
			//Console.WriteLine();

			//File.Delete(FileName);

			//Console.WriteLine($"File writer buffer size {FileBufferSize} and row buffer size {RowBufferSize}");

			//for (var i = 0; i < count; i++) {
			//	times[i] = CreateFile(sw, RowBufferSize, FileBufferSize);
			//}

			//var avg = new TimeSpan(times.Sum(x => x.Ticks) / count);
			//Console.WriteLine($"Avg: {avg:mm':'ss':'fff}");
			//Console.WriteLine();
			Console.ReadKey();
		}

		//private static TimeSpan CreateFile(Stopwatch sw, int rowBuffer, int fileBuffer) {
		//	sw.Restart();
		//	MeasurementsGenerator.CreateFile(FileName, RowSize, rowBuffer, fileBuffer);
		//	sw.Stop();

		//	Console.WriteLine($"Time: {sw.Elapsed:mm':'ss':'fff}");
		//	return sw.Elapsed;
		//}
	}
}