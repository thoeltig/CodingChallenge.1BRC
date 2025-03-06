using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using _1BRC.Framework.Console.Generate;

namespace _1BRC.Framework.Console.Read {
	internal static class MeasurementsReader {
		public static unsafe IReadOnlyCollection<TemperatureContainer> ReadFile(string filePath) {
			var filePtr = NativeMethods.CreateFile(filePath, NativeMethods.GenericRead, FileShare.None, IntPtr.Zero, FileMode.Open, NativeMethods.FileAttributeNormal, IntPtr.Zero);
			if (filePtr.ToInt32() == NativeMethods.InvalidHandleValue) {
				return Array.Empty<TemperatureContainer>();
			}

			#if DEBUG
			const int maxParallel = 1;
			#else
			var maxParallel = Environment.ProcessorCount;
			#endif

			const int blockSize = 262144;
			var count = maxParallel * 2;
			var tasks = new Task[maxParallel];
			var bytePointers = new byte*[count];
			var handles = new GCHandle[count];
			for (var i = 0; i < count; i += 2) {
				var arr = new byte[blockSize];
				var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
				handles[i] = handle;
				bytePointers[i] = (byte*)handle.AddrOfPinnedObject().ToPointer();

				arr = new byte[106];
				handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
				handles[i + 1] = handle;
				bytePointers[i + 1] = (byte*)handle.AddrOfPinnedObject().ToPointer();
			}

			var offset = 0;
			var isRunning = true;
			var dic = new Dictionary<uint, TemperatureContainer>();

			do {
				for (var i = 0; i < maxParallel; i++) {
					var ptr = bytePointers[i * 2];
					if (NativeMethods.ReadFile(filePtr, ptr + offset, blockSize - offset, out var readBytes, IntPtr.Zero) != 0 && readBytes != 0) {
						isRunning = readBytes == blockSize - offset;
						tasks[i] = ReadLinesAsync(isRunning, dic, ptr, readBytes + offset, bytePointers[i * 2 + 1]);
						offset = 0;
					} else {
						tasks[i] = Task.FromResult(0);
					}
				}

				Task.WaitAll(tasks);

				offset = tasks[0] is Task<int> t ? t.Result : 0;
				for (var i = 1; i < maxParallel; i++) {
					if (tasks[i] is not Task<int> resultTask || resultTask.Result == 0) {
						continue;
					}

					var length = resultTask.Result;
					NativeMethods.CopyMemory(bytePointers[0] + offset, bytePointers[i * 2], (uint)length);
					offset += length;
				}
			} while (isRunning);

			NativeMethods.CloseHandle(filePtr);

			for (var i = 0; i < handles.Length; i++) {
				handles[i].Free();
			}

			Array.Clear(bytePointers, 0, bytePointers.Length);
			Array.Clear(handles, 0, handles.Length);
			Array.Clear(tasks, 0, tasks.Length);
			return dic.Values;
		}

		private static unsafe Task<int> ReadLinesAsync(bool ignoreStartAndEndPart, Dictionary<uint, TemperatureContainer> dic, byte* blockPtr, int readBytes, byte* linePtr) {
			const byte lineSeparator = 59; // ;
			const byte signedSymbol = 45; // -
			const byte temperatureSeparator = 46; // .
			const byte newLine = 10; // \n

			var idx = 0;
			var nameLength = 0;
			var flag = IntParseFlag.None;
			var temperature = 0;
			int? firstLineEndIdx = null;
			int? lastLineEndIdx = null;

			for (var bufferIdx = 0; bufferIdx < readBytes; bufferIdx++) {
				var value = blockPtr[bufferIdx];

				switch (value) {
					case lineSeparator: {
						nameLength = idx;
						break;
					}
					case temperatureSeparator: {
						flag |= IntParseFlag.HasDot;
						break;
					}
					case signedSymbol: {
						flag |= IntParseFlag.Signed;
						break;
					}
					case newLine: {
						if (firstLineEndIdx.HasValue == false && ignoreStartAndEndPart) {
							firstLineEndIdx = bufferIdx;
							idx = 0;
							flag = IntParseFlag.None;
							continue;
						}

						lastLineEndIdx = bufferIdx;

						// get key with hash logic
						var key = 2166136261;
						unchecked {
							for (var i = 0; i < nameLength; i++) {
								key = (key * 16777619) ^ linePtr[i];
							}
						}

						// parse int -> double or single digit with or without fraction
						switch (flag) {
							case IntParseFlag.Signed | IntParseFlag.HasDot: {
								temperature = -((idx - nameLength) switch {
									3 => (linePtr[nameLength] - 48) * 100 + (linePtr[nameLength + 1] - 48) * 10 + linePtr[nameLength + 2] - 48,
									_ => (linePtr[nameLength] - 48) * 10 + (linePtr[nameLength + 1] - 48)
								});
								break;
							}
							case IntParseFlag.HasDot: {
								temperature = (idx - nameLength) switch {
									3 => (linePtr[nameLength] - 48) * 100 + (linePtr[nameLength + 1] - 48) * 10 + linePtr[nameLength + 2] - 48,
									_ => (linePtr[nameLength] - 48) * 10 + (linePtr[nameLength + 1] - 48)
								};
								break;
							}
							case IntParseFlag.Signed: {
								temperature = -10 * (idx - nameLength) switch {
									2 => (linePtr[nameLength] - 48) * 10 + linePtr[nameLength + 1] - 48,
									_ => linePtr[nameLength] - 48
								};
								break;
							}
							case IntParseFlag.None: {
								temperature = 10 * (idx - nameLength) switch {
									2 => (linePtr[nameLength] - 48) * 10 + linePtr[nameLength + 1] - 48,
									_ => linePtr[nameLength] - 48
								};
								break;
							}
						}

						if (dic.TryGetValue(key, out var container)) {
							container.Update(temperature);
						} else {
							dic.Add(key, new TemperatureContainer(Encoding.UTF8.GetString(linePtr, nameLength), temperature));
						}

						idx = 0;
						flag = IntParseFlag.None;
						break;
					}
					default: {
						linePtr[idx++] = value;
						break;
					}
				}
			}

			var firstIdx = firstLineEndIdx ?? 0;
			var lastIdx = lastLineEndIdx ?? 0;
			if (ignoreStartAndEndPart == false || (firstIdx == 0 && lastIdx == 0)) {
				return Task.FromResult(0);
			}

			var lastCount = 0;
			if (lastIdx != 0) {
				lastCount = readBytes - lastIdx;
			}

			if (lastCount != 0) {
				NativeMethods.CopyMemory(blockPtr + firstIdx, blockPtr + lastIdx, (uint)lastCount);
			}

			return Task.FromResult(firstIdx + lastCount);
		}

		[Flags]
		private enum IntParseFlag : byte {
			None = 0,
			Signed = 1,
			HasDot = 2
		}
	}

	internal class TemperatureContainer {
		private const double Divider = 10.0;

		#region

		private int _min;
		private int _sum;
		private int _max;
		private int _count;

		#endregion

		public TemperatureContainer(string name, int temperature) {
			Name = name;
			_sum = temperature;
			_min = temperature;
			_max = temperature;
			_count = 1;
		}

		public string Name { get; }

		public double Min => _min / Divider;

		public double Average => _sum / Divider / _count;

		public double Max => _max / Divider;

		public void Update(int temperature) {
			_sum += temperature;
			_count++;

			if (temperature < _min) {
				_min = temperature;
			} else if (temperature > _count) {
				_max = temperature;
			}
		}
	}
}