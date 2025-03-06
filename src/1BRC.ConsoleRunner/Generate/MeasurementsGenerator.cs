using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace _1BRC.Framework.Console.Generate {
	internal class MeasurementsGenerator {
		public static unsafe void GenerateFile(string filePath, int totalRowCount) {
			var filePtr = NativeMethods.CreateFile(filePath, NativeMethods.GenericWrite, FileShare.None, IntPtr.Zero, FileMode.OpenOrCreate, NativeMethods.FileAttributeNormal, IntPtr.Zero);
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
			var sliceOfBlock = (int)Math.Ceiling(blockSize / (double)maxParallel);

			var bytePointers = new byte*[maxParallel];
			var pointers = new IntPtr[maxParallel];
			var handles = new GCHandle[maxParallel];
			for (var i = 0; i < maxParallel; i++) {
				var arr = new byte[sliceOfBlock];
				var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
				handles[i] = handle;
				var ptr = (byte*)handle.AddrOfPinnedObject().ToPointer();
				bytePointers[i] = ptr;
				pointers[i] = new IntPtr(ptr);
			}

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
			var nameIndex = 0;
			var numberIndex = 0;

			fixed (byte* blockPtr = block) {
				var ptr = new IntPtr(blockPtr);
				while (linesToCreate > 0) {
					for (var j = 0; j < maxParallel; j++) {
						tasks[j] = GenerateLinesAsync(bytePointers[j], sliceOfBlock, CanCreateLine, GetName, GetNumber);
					}

					Task.WaitAll(tasks);

					uint blockIndex = 0;
					for (var j = 0; j < maxParallel; j++) {
						var length = ((Task<uint>)tasks[j]).Result;
						NativeMethods.CopyMemory(IntPtr.Add(ptr, (int)blockIndex), pointers[j], length);
						blockIndex += length;
					}

					NativeMethods.WriteFile(filePtr, blockPtr, blockIndex, out _, IntPtr.Zero);
				}
			}

			NativeMethods.CloseHandle(filePtr);

			for (var i = 0; i < maxParallel; i++) {
				handles[i].Free();
			}

			Array.Clear(pointers, 0, pointers.Length);
			Array.Clear(bytePointers, 0, bytePointers.Length);
			Array.Clear(handles, 0, handles.Length);
			return;

			bool CanCreateLine() => --linesToCreate > 0;

			byte[] GetName() {
				var idx = ++nameIndex;
				if (idx < maxNameCount) {
					return names[idx];
				}

				nameIndex = 0;
				return names[0];
			}

			byte GetNumber() {
				var idx = ++numberIndex;
				if (idx < numberBytesForTemperatureCount) {
					return numberBytesForTemperature[idx];
				}

				numberIndex = 0;
				return numberBytesForTemperature[0];
			}
		}

		private static unsafe Task<uint> GenerateLinesAsync(byte* ptr, int capacity, Func<bool> canCreatedAnotherLine, Func<byte[]> getName, Func<byte> getNumber) {
			const int maxLineLength = 106;
			uint idx = 0;

			while (canCreatedAnotherLine() && capacity - idx >= maxLineLength) {
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
	}
}