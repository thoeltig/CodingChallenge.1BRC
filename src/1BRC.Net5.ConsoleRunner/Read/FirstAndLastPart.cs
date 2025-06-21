namespace _1BRC.Net9.ConsoleRunner.Read {
	internal struct FirstAndLastPart {
		public int FirstPartCount { get; }

		public int LastPartIndex { get; }

		public int LastPartCount { get; }

		public FirstAndLastPart(int? firstPartCount, int? lastPartIndex, int count)
			: this() {
			if (firstPartCount.HasValue) {
				FirstPartCount = firstPartCount.Value;
			}

			if (lastPartIndex.HasValue) {
				LastPartIndex = lastPartIndex.Value;
				LastPartCount = count - lastPartIndex.Value;
			}
		}
	}
}