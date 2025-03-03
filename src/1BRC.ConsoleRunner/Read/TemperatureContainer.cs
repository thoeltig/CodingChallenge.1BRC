namespace _1BRC.Framework.Console.Read {
	internal class TemperatureContainer {
		public double Min { get; private set; }

		public double Average { get; private set; }

		public double Max { get; private set; }

		public TemperatureContainer(double temperature) {
			Average = temperature;
			Min = temperature;
			Max = temperature;
		}

		public void Update(double temperature) {
			Average = (Average + temperature) / 2.0;

			if (temperature < Min) {
				Min = temperature;
			} else if (temperature > Max) {
				Max = temperature;
			}
		}
	}
}