using System;
using System.Threading;
using System.Threading.Tasks;

namespace _1BRC.Framework.Console.Generate {
	internal class MeasurementsGenerator {
		public static unsafe void GenerateFile(string filePath, int totalRowCount) {
			var filePtr = NativeMethods.CreateFile(filePath, NativeMethods.GenericWrite, NativeMethods.FileShareNone, IntPtr.Zero, NativeMethods.CreateAlways, NativeMethods.FileAttributeNormal, IntPtr.Zero);
			if (filePtr.ToInt32() == NativeMethods.InvalidHandleValue) {
				return;
			}

			#if DEBUG
			const int maxParallel = 1;
			#else
			var maxParallel = Environment.ProcessorCount;
			#endif

			const int blockSize = 16777216;
			var tasks = new Task[maxParallel];
			var block = new byte[blockSize];
			var names = WeatherStation.Names;
			var maxNameCount = names.Length;
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
			var linesToCreate = totalRowCount;
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
				var idx = Interlocked.Decrement(ref linesToCreate);
				if (idx < 0) {
					return -1;
				}

				idx = blockIndex;
				var newIdx = Interlocked.Add(ref blockIndex, lineLength);
				if (blockSize - newIdx >= 0) {
					return idx;
				}

				Interlocked.Exchange(ref blockIndex, idx);
				Interlocked.Increment(ref linesToCreate);
				return -1;
			}

			byte[] GetName() {
				if (++nameIndex < maxNameCount) {
					return names[nameIndex];
				}

				nameIndex = 0;
				return names[0];
			}

			byte GetNumber() {
				if (++numberIndex < numberBytesForTemperatureCount) {
					return numberBytesForTemperature[numberIndex];
				}

				numberIndex = 0;
				return numberBytesForTemperature[0];
			}
		}

		private static unsafe Task GenerateLinesAsync(byte* ptr, Func<int, int> getBlockIndexFunc, Func<byte[]> getName, Func<byte> getNumber) {
			const byte zeroByte = 48; // 0
			const byte lineSeparator = 59; // ;
			const byte signedSymbol = 45; // -
			const byte temperatureSeparator = 46; // .
			const byte newLine = 10; // \n

			for (;;) {
				// plan line
				var name = getName();
				var nameLength = name.Length;
				var fractionValue = getNumber();
				var singleDigit = getNumber();
				var doubleDigit = getNumber();
				var useDoubleDigit = doubleDigit != zeroByte;
				var useSignedSymbol = (singleDigit != zeroByte || useDoubleDigit) && nameLength % 2 == 0;
				var useFraction = fractionValue != zeroByte;

				var lineLength = nameLength + (useSignedSymbol ? 2 : 1) + (useDoubleDigit ? 2 : 1) + (useFraction ? 3 : 2);
				var idx = getBlockIndexFunc(lineLength);
				if (idx == -1) {
					break;
				}

				for (var i = 0; i < nameLength; i++) {
					ptr[idx++] = name[i];
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
	}
}