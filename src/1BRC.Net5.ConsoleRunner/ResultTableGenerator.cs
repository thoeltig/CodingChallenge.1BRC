using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace _1BRC.Net5.ConsoleRunner {
	internal class ResultTableGenerator {
		#region

		private readonly Dictionary<string, DataRow> _rows = new();

		#endregion

		public void Add(FileOptions option, int chunkSize, int bufferSize, TimeSpan? elapsedTime, long usedMemoryInBytes) {
			const string separator = "_";
			var identifier = string.Concat(chunkSize, separator, bufferSize);

			if (_rows.TryGetValue(identifier, out var row)) {
				row.Add(option, elapsedTime, usedMemoryInBytes);
			} else {
				_rows.Add(identifier, new DataRow(chunkSize, bufferSize, option, elapsedTime, usedMemoryInBytes));
			}
		}

		public string PrintTable(string tableName, int totalFileSize) {
			var rowCount = _rows.Count;
			if (rowCount == 0) {
				return string.Empty;
			}

			var firstRow = _rows.Values.First();
			var columnCount = firstRow.Cells.Count;
			if (columnCount == 0) {
				return string.Empty;
			}

			const string separator = "|";
			const string separatorHeader = "----";
			const string percentage = "%";
			const string openBracket = " (";
			const string closeBracket = ")";
			var builder = new StringBuilder();

			// Write header column
			builder.Append(separator);
			builder.Append(tableName);
			builder.Append(openBracket);
			builder.Append("");
			builder.Append(Math.Round(_rows.Values.Sum(x => x.Cells.Values.Sum(y => y.UsedMemoryBytes) / (double)x.Cells.Count) / _rows.Count / 1024.0 / 1024.0, 3));
			builder.Append(" MB on average");
			builder.Append(closeBracket);
			builder.Append(closeBracket);
			builder.Append(separator);

			foreach (var columnName in firstRow.Cells.Keys) {
				builder.Append(columnName);
				builder.Append(separator);
				builder.Append(percentage);
				builder.Append(separator);
			}

			builder.AppendLine();

			// Write separator row between header and data
			builder.Append(separator);
			builder.Append(separatorHeader);
			builder.Append(separator);

			for (var i = 0; i < columnCount; i++) {
				builder.Append(separatorHeader);
				builder.Append(separator);
				builder.Append(separatorHeader);
				builder.Append(separator);
			}

			builder.AppendLine();

			// Write data rows
			var firstTime = 0.0;
			var hasFirstTime = false;
			foreach (var row in _rows.Values.OrderBy(x => x.BufferSize)) {
				if (hasFirstTime == false) {
					var firstCell = row.Cells.Values.FirstOrDefault(x => x.ElapsedTime.HasValue);
					if (firstCell != null) {
						firstTime = firstCell.ElapsedTime.Value.TotalSeconds;
						hasFirstTime = true;
					}
				}

				// Description
				const string bufferDesc = "Buffer ";
				const string chunkDesc = " Chunk ";
				const string times = " times";
				builder.Append(separator);
				builder.Append(bufferDesc);
				builder.Append(row.BufferSize);
				builder.Append(chunkDesc);
				builder.Append(row.ChunkSize);
				builder.Append(openBracket);
				builder.Append((int)Math.Ceiling(totalFileSize / (double)row.ChunkSize));
				builder.Append(times);
				builder.Append(closeBracket);
				builder.Append(separator);

				foreach (var cell in row.Cells) {
					if (cell.Value.ElapsedTime.HasValue == false) {
						const string invalidSymbol = "-";
						builder.Append(invalidSymbol);
						builder.Append(separator);
						builder.Append(invalidSymbol);
						builder.Append(separator);
						continue;
					}

					const string timeFormat = "ss':'fffffff";
					var timeSpan = cell.Value.ElapsedTime.Value;
					builder.Append(timeSpan.ToString(timeFormat));
					builder.Append(separator);
					builder.Append(Math.Round((timeSpan.TotalSeconds - firstTime) / firstTime * 100, 2).ToString(CultureInfo.InvariantCulture));
					builder.Append(separator);
				}

				builder.AppendLine();
			}

			return builder.ToString();
		}

		private class DataRow {
			#region

			private readonly Dictionary<FileOptions, DataCell> _cells;

			#endregion

			public DataRow(int chunkSize, int bufferSize) {
				ChunkSize = chunkSize;
				BufferSize = bufferSize;
				_cells = new Dictionary<FileOptions, DataCell>();
			}

			public DataRow(int chunkSize, int bufferSize, FileOptions option, TimeSpan? elapsedTime, long usedMemoryInBytes)
				: this(chunkSize, bufferSize) {
				Add(option, elapsedTime, usedMemoryInBytes);
			}

			public int ChunkSize { get; }

			public int BufferSize { get; }

			public IReadOnlyDictionary<FileOptions, DataCell> Cells => _cells;

			public void Add(FileOptions option, TimeSpan? elapsedTime, long usedMemoryInBytes) => _cells[option] = new DataCell(elapsedTime, usedMemoryInBytes);
		}

		private class DataCell {
			public DataCell(TimeSpan? elapsedTime, long usedMemoryBytes) {
				ElapsedTime = elapsedTime;
				UsedMemoryBytes = usedMemoryBytes;
			}

			public TimeSpan? ElapsedTime { get; }

			public long UsedMemoryBytes { get; }
		}
	}
}