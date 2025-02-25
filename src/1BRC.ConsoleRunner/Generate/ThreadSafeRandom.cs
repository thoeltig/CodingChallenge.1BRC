using System;
using System.Threading;

namespace _1BRC.Framework.ConsoleRunner.Generate {
	internal static class ThreadSafeRandom {
		private static readonly Random _random = new();
		private static readonly object _lock = new();
		private static readonly ThreadLocal<Random> _threadRandom = new(NewRandom);

		public static Random Instance => _threadRandom.Value;

		private static Random NewRandom() {
			lock (_lock) {
				return new Random(_random.Next());
			}
		}
	}
}