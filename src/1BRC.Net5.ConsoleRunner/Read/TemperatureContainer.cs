namespace _1BRC.ConsoleRunner.Read {
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
			} else if (temperature > _max) {
				_max = temperature;
			}
		}
	}
}