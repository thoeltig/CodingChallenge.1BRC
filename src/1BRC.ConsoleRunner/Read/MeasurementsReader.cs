using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _1BRC.Framework.Console.Generate;

namespace _1BRC.Framework.Console.Read {
	internal static class MeasurementsReader {
		public static unsafe IReadOnlyCollection<TemperatureContainer> ReadFile(string filePath) {
			var filePtr = NativeMethods.CreateFile(filePath, NativeMethods.GenericRead, FileShare.None, IntPtr.Zero, FileMode.Open, NativeMethods.FileAttributeNormal, IntPtr.Zero);
			if (filePtr.ToInt32() == NativeMethods.InvalidHandleValue) {
				return Array.Empty<TemperatureContainer>();
			}

			const byte lineSeparator = 59; // ;
			const byte signedSymbol = 45; // -
			const byte temperatureSeparator = 46; // .
			const byte newLine = 10; // \n

			var dic = new Dictionary<uint, TemperatureContainer>();
			var line = new byte[106];
			var idx = 0;
			var nameLength = 0;
			var flag = IntParseFlag.None;
			var temperature = 0;
			const int bufferSize = 262144;
			var block = new byte[bufferSize];

			fixed (byte* blockPtr = block) {
				while (NativeMethods.ReadFile(filePtr, blockPtr, bufferSize, out var readBytes, IntPtr.Zero) != 0 && readBytes != 0) {
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
								// get key with hash logic
								var key = 2166136261;
								unchecked {
									for (var i = 0; i < nameLength; i++) {
										key = (key * 16777619) ^ line[i];
									}
								}

								// parse int -> double or single digit with or without fraction
								switch (flag) {
									case IntParseFlag.Signed | IntParseFlag.HasDot: {
										temperature = -((idx - nameLength) switch {
											3 => (line[nameLength] - 48) * 100 + (line[nameLength + 1] - 48) * 10 + line[nameLength + 2] - 48,
											_ => (line[nameLength] - 48) * 10 + (line[nameLength + 1] - 48)
										});
										break;
									}
									case IntParseFlag.HasDot: {
										temperature = (idx - nameLength) switch {
											3 => (line[nameLength] - 48) * 100 + (line[nameLength + 1] - 48) * 10 + line[nameLength + 2] - 48,
											_ => (line[nameLength] - 48) * 10 + (line[nameLength + 1] - 48)
										};
										break;
									}
									case IntParseFlag.Signed: {
										temperature = -10 * ((idx - nameLength) switch {
											2 => (line[nameLength] - 48) * 10 + line[nameLength + 1] - 48,
											_ => line[nameLength] - 48
										});
										break;
									}
									case IntParseFlag.None: {
										temperature = 10 * ((idx - nameLength) switch {
											2 => (line[nameLength] - 48) * 10 + line[nameLength + 1] - 48,
											_ => line[nameLength] - 48
										});
										break;
									}
								}

								if (dic.TryGetValue(key, out var container)) {
									container.Update(temperature);
								} else {
									dic.Add(key, new TemperatureContainer(Encoding.UTF8.GetString(line, 0, nameLength), temperature));
								}

								idx = 0;
								flag = IntParseFlag.None;
								break;
							}
							default: {
								line[idx++] = value;
								break;
							}
						}
					}

				}
			}

			NativeMethods.CloseHandle(filePtr);
			return dic.Values;
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
		private int _count;
		private int _min;
		private int _max;
		private int _sum;

		public string Name { get; }

		public double Min => _min / Divider;

		public double Average => _sum / Divider / _count;

		public double Max => _max / Divider;

		public TemperatureContainer(string name, int temperature) {
			Name = name;
			_sum = temperature;
			_min = temperature;
			_max = temperature;
			_count = 1;
		}

		public void Update(int temperature) {
			_sum += temperature;
			_count++;

			if (temperature < _min) {
				_min = temperature;
			} else if (temperature > _max) {
				_max = temperature;
			}
		}
	}
}