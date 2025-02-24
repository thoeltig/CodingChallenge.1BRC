using System;
using System.Diagnostics;
using _1BRC.Net5.ConsoleRunner.Test;

namespace _1BRC.Net5.ConsoleRunner {
	internal class Program {
		private static void Main(string[] args) {
			Console.WriteLine("File write test started");
			var sw = new Stopwatch();
			sw.Start();
			var tester = new FileReadAndWrite();
			tester.ExecuteTest(Console.WriteLine);
			sw.Stop();
			Console.WriteLine($"File write test done! Took {sw.Elapsed:mm':'ss':'fff} to finish!");
			Console.ReadKey();
		}
	}
}