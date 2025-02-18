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

		public void Add(FileOptions option, int chunkSize, int bufferSize, TimeSpan? elapsedTime) {
			const string separator = "_";
			var identifier = string.Concat(chunkSize, separator, bufferSize);

			if (_rows.TryGetValue(identifier, out var row)) {
				row.Add(option, elapsedTime);
			} else {
				_rows.Add(identifier, new DataRow(chunkSize, bufferSize, option, elapsedTime));
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
			var builder = new StringBuilder();

			// Write header column
			builder.Append(separator);
			builder.Append(tableName);
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
					firstTime = row.Cells.Values.First().Value.TotalSeconds;
					hasFirstTime = true;
				}

				// Description
				const string bufferDesc = "Buffer ";
				const string chunkDesc = " Chunk ";
				const string openBracket = " (";
				const string closeBracket = " times)";
				builder.Append(separator);
				builder.Append(bufferDesc);
				builder.Append(row.BufferSize);
				builder.Append(chunkDesc);
				builder.Append(row.ChunkSize);
				builder.Append(openBracket);
				builder.Append((int)Math.Ceiling(totalFileSize / (double)row.ChunkSize));
				builder.Append(closeBracket);
				builder.Append(separator);

				foreach (var cell in row.Cells) {
					if (cell.Value.HasValue == false) {
						const string invalidSymbol = "-";
						builder.Append(invalidSymbol);
						builder.Append(separator);
						builder.Append(invalidSymbol);
						builder.Append(separator);
						continue;
					}

					const string timeFormat = "ss':'fffffff";
					var timeSpan = cell.Value.Value;
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

			private readonly Dictionary<FileOptions, TimeSpan?> _cells;

			#endregion

			public DataRow(int chunkSize, int bufferSize) {
				ChunkSize = chunkSize;
				BufferSize = bufferSize;
				_cells = new Dictionary<FileOptions, TimeSpan?>();
			}

			public DataRow(int chunkSize, int bufferSize, FileOptions option, TimeSpan? elapsedTime)
				: this(chunkSize, bufferSize) {
				Add(option, elapsedTime);
			}

			public int ChunkSize { get; }

			public int BufferSize { get; }

			public IReadOnlyDictionary<FileOptions, TimeSpan?> Cells => _cells;

			public void Add(FileOptions option, TimeSpan? elapsedTime) => _cells[option] = elapsedTime;
		}
	}
}