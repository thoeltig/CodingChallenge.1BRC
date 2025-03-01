using System;
using System.Threading;
using System.Threading.Tasks;

namespace _1BRC.Framework.Console.Generate {
	internal class MeasurementsGenerator {
		private const int MaxNameCount = 10000;
		private const int BlockSize = 16777216;
		private const int MaxLineLength = 106;

		public static unsafe void GenerateFile(string filePath, int totalRowCount) {
			var filePtr = NativeMethods.CreateFile(filePath, NativeMethods.GenericWrite, NativeMethods.FileShareNone, IntPtr.Zero, NativeMethods.CreateAlways, NativeMethods.FileAttributeNormal, IntPtr.Zero);
			if (filePtr.ToInt32() == NativeMethods.InvalidHandleValue) {
				return;
			}

			var names = CreateRandomNames();
			var numberBytesForTemperature = new byte[] {
				48, // 0
				49, // 1
				50, // 2
				51, // 3
				52, // 4
				53, // 5
				54, // 6
				55, // 7
				56, // 8
				57 // 9
			};
			var numberBytesForTemperatureCount = numberBytesForTemperature.Length;

			#if DEBUG
			const int maxParallel = 1;
			#else
			var maxParallel = Environment.ProcessorCount;
			#endif

			var linesToCreate = totalRowCount;
			var tasks = new Task[maxParallel];
			var block = new byte[BlockSize];
			var blockIndex = 0;
			var nameIndex = 0;
			var numberIndex = 0;

			fixed (byte* blockPtr = block) {
				while (linesToCreate > 0) {
					for (var j = 0; j < maxParallel; j++) {
						tasks[j] = GenerateLinesAsync(blockPtr, GetBlockIndex, GetName, GetNumber);
					}

					Task.WaitAll(tasks);
					NativeMethods.WriteFile(filePtr, blockPtr, (uint)blockIndex, out _, IntPtr.Zero);
					blockIndex = 0;
				}
			}

			NativeMethods.CloseHandle(filePtr);
			return;

			int GetBlockIndex(int lineLength) {
				// reserve line
				var localLinesToCreate = Interlocked.Decrement(ref linesToCreate);
				if (localLinesToCreate < 0) {
					return -1;
				}

				// check if reserved line would fit
				var localBlockIndex = Interlocked.Add(ref blockIndex, lineLength);

				if (BlockSize - localBlockIndex >= MaxLineLength) {
					return localBlockIndex - lineLength;
				}

				// free reserved line if it would not fit
				Interlocked.Increment(ref linesToCreate);
				Interlocked.Add(ref blockIndex, -lineLength);
				return -1;
			}

			byte[] GetName() {
				var idx = Interlocked.Increment(ref nameIndex);
				if (idx < MaxNameCount) {
					return names[idx];
				}

				Interlocked.Exchange(ref nameIndex, 0);
				return names[0];
			}

			byte GetNumber() {
				var idx = Interlocked.Increment(ref numberIndex);
				if (idx < numberBytesForTemperatureCount) {
					return numberBytesForTemperature[idx];
				}

				Interlocked.Exchange(ref numberIndex, 0);
				return numberBytesForTemperature[0];
			}
		}

		private static unsafe Task GenerateLinesAsync(byte* ptr, Func<int, int> getBlockIndexFunc, Func<byte[]> getName, Func<byte> getNumber) {
			const byte lineSeparator = 59; // ;
			const byte zeroByte = 48; // 0
			const byte signedSymbol = 45; // -
			const byte temperatureSeparator = 46; // .
			const byte newLine = 10; // \n

			for (var idx = 0;;) {
				// plan line
				var name = getName();
				var fractionValue = getNumber();
				var singleDigit = getNumber();
				var doubleDigit = getNumber();
				var useDoubleDigit = doubleDigit != zeroByte;
				var useSignedSymbol = (singleDigit != zeroByte || useDoubleDigit) && idx % 3 == 0;
				var useFraction = fractionValue != zeroByte;

				var lineLength = name.Length + (useSignedSymbol ? 2 : 1) + (useDoubleDigit ? 2 : 1) + (useFraction ? 3 : 2);
				idx = getBlockIndexFunc(lineLength);
				if (idx == -1) {
					break;
				}

				for (var i = 0; i < name.Length; i++, idx++) {
					ptr[idx] = name[i];
				}

				ptr[idx++] = lineSeparator;

				if (useSignedSymbol) {
					ptr[idx++] = signedSymbol;
				}

				if (useDoubleDigit) {
					ptr[idx++] = doubleDigit;
				}

				ptr[idx++] = singleDigit;

				if (useFraction) {
					ptr[idx++] = temperatureSeparator;
					ptr[idx++] = fractionValue;
				}

				ptr[idx] = newLine;
			}

			return Task.CompletedTask;
		}

		private static byte[][] CreateRandomNames() {
			const byte arraySize = 253; // 3 bytes will be ignored
			const byte firstIgnoreValue = 10; // \n
			const byte secondIgnoreValue = 13; // \r
			const byte thirdIgnoreValue = 59; // ;

			var validNameBytes = new byte[arraySize];
			for (int i = 0, j = 0; i <= byte.MaxValue && j < arraySize; i++) {
				var nextByte = (byte)i;
				if (nextByte is firstIgnoreValue or secondIgnoreValue or thirdIgnoreValue) {
					continue;
				}

				validNameBytes[j++] = nextByte;
			}

			var names = new byte[MaxNameCount][];
			var nameIndex = 0;

			Parallel.For(0, MaxNameCount, i => {
				const int randomMinIncluded = 7; // Name needs at least 1 character
				//const int randomMaxExcluded = 100 + 1; // Name can have up to 100 characters

				var nameLength = randomMinIncluded; // ThreadSafeRandom.Instance.Next(randomMinIncluded, randomMaxExcluded);
				var bytes = new byte[nameLength];
				for (var j = 0; j < nameLength; j++) {
					var idx = Interlocked.Increment(ref nameIndex);
					if (idx >= arraySize) {
						Interlocked.Exchange(ref nameIndex, 0);
						idx = 0;
					}

					bytes[j] = validNameBytes[idx];
				}

				names[i] = bytes;
			});

			return names;
		}
	}
}