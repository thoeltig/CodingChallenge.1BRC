using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace _1BRC.Framework.Console.Generate {
	internal class MeasurementsGenerator {
		private const int MaxNameCount = 10000;

		public static unsafe void GenerateFile(string filePath, int totalRowCount) {
			var filePtr = NativeMethods.CreateFile(filePath, NativeMethods.GenericWrite, NativeMethods.FileShareNone, IntPtr.Zero, NativeMethods.CreateAlways, NativeMethods.FileAttributeNormal, IntPtr.Zero);
			if (filePtr == (IntPtr)NativeMethods.InvalidHandleValue) {
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

			const int blockSize = 16777216;
			var block = new byte[blockSize];
			var sliceOfBlock = (int)Math.Ceiling(blockSize / (double)maxParallel);

			var bytePointers = new byte*[maxParallel];
			var pointers = new IntPtr[maxParallel];
			var handles = new GCHandle[maxParallel];
			for (var i = 0; i < maxParallel; i++) {
				var arr = new byte[sliceOfBlock];
				var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
				handles[i] = handle;
				var address = handle.AddrOfPinnedObject();
				var ptr = (byte*)address.ToPointer();
				bytePointers[i] = ptr;
				pointers[i] = new IntPtr(ptr);
			}

			var nameIndex = 0;
			var numberIndex = 0;
			var linesToCreate = totalRowCount;
			var tasks = new Task[maxParallel];

			fixed (byte* blockPtr = block) {
				var ptr = new IntPtr(blockPtr);
				while (linesToCreate > 0) {
					for (var j = 0; j < maxParallel; j++) {
						tasks[j] = GenerateLinesAsync(bytePointers[j], sliceOfBlock, CanCreateLine, GetName, GetNumber);
					}

					Task.WaitAll(tasks);

					var blockIndex = 0;
					for (var j = 0; j < maxParallel; j++) {
						var length = ((Task<int>)tasks[j]).Result;
						NativeMethods.CopyMemory(IntPtr.Add(ptr, blockIndex), pointers[j], (uint)length);
						blockIndex += length;
					}

					NativeMethods.WriteFile(filePtr, blockPtr, (uint)blockIndex, out _, IntPtr.Zero);
				}
			}

			NativeMethods.CloseHandle(filePtr);

			for (var i = 0; i < maxParallel; i++) {
				handles[i].Free();
			}

			Array.Clear(pointers, 0, maxParallel);
			Array.Clear(bytePointers, 0, maxParallel);
			Array.Clear(handles, 0, maxParallel);
			return;

			bool CanCreateLine() {
				if (linesToCreate <= 0) {
					return false;
				}

				linesToCreate--;
				return true;
			}

			byte[] GetName() {
				nameIndex = nameIndex + 1 < MaxNameCount ? nameIndex + 1 : 0;
				return names[nameIndex];
			}

			byte GetNumber() {
				numberIndex = numberIndex + 1 < numberBytesForTemperatureCount ? numberIndex + 1 : 0;
				return numberBytesForTemperature[numberIndex];
			}
		}

		private static unsafe Task<int> GenerateLinesAsync(byte* ptr, int capacity, Func<bool> canCreatedAnotherLine, Func<byte[]> getName, Func<byte> getNumber) {
			const int maxLineLength = 106;
			var idx = 0;

			while (canCreatedAnotherLine() && capacity - idx > maxLineLength) {
				// Write name to block
				var name = getName();
				for (var i = 0; i < name.Length; i++, idx++) {
					ptr[idx] = name[i];
				}

				const byte lineSeparator = 59; // ;
				ptr[idx++] = lineSeparator;

				// Create temperature
				// The random value range needs to be 1 to 1999 because the result will be divided by 10 and afterwards 100 is subtracted
				// which will result in the new value range from -99.9 to 99.9
				const byte zeroByte = 48; // 0
				var fractionValue = getNumber();
				var singleDigit = getNumber();
				var doubleDigit = getNumber();
				var useDoubleDigit = doubleDigit != zeroByte;

				// Write temperature to block
				if ((singleDigit != zeroByte || useDoubleDigit) && idx % 3 == 0) {
					const byte signedSymbol = 45; // -
					ptr[idx++] = signedSymbol;
				}

				if (useDoubleDigit) {
					ptr[idx++] = doubleDigit;
				}

				ptr[idx++] = singleDigit;

				if (fractionValue != zeroByte) {
					const byte temperatureSeparator = 46; // .
					ptr[idx++] = temperatureSeparator;
					ptr[idx++] = fractionValue;
				}

				// Write new line to block
				const byte newLine = 10; // \n
				ptr[idx++] = newLine;
			}

			return Task.FromResult(idx);
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

				validNameBytes[j] = nextByte;
				j++;
			}

			var names = new byte[MaxNameCount][];

			Parallel.For(0, MaxNameCount, i => {
				const int randomMinIncluded = 7; // Name needs at least 1 character
				//const int randomMaxExcluded = 100 + 1; // Name can have up to 100 characters

				var nameLength = randomMinIncluded; // ThreadSafeRandom.Instance.Next(randomMinIncluded, randomMaxExcluded);
				var bytes = new byte[nameLength];
				var nameIndex = 0;
				for (var j = 0; j < nameLength; j++) {
					bytes[j] = validNameBytes[nameIndex];
					nameIndex = nameIndex + 1 < arraySize ? nameIndex + 1 : 0;
				}

				names[i] = bytes;
			});

			return names;
		}
	}
}