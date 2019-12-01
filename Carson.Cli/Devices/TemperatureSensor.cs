using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Experiment1.ZWaveDrivers;

namespace Experiment1
{
	public class TemperatureSensor : IDevice
	{
		/// <summary>
		/// Called when the temperature sensor value changes
		/// </summary>
		public Action<IDevice, SimpleSensorState<float>> OnUpdate { get; set; }

		/// <summary>
		/// The current state of the temperature sensor
		/// </summary>
		public SimpleSensorState<float> State { get; set; }

		public string Name { get; set; }

		public string Plural { get; set; }

		public Area Area { get; set; }

		public bool IsDead { get; set; }

		readonly IZWaveTemperatureDriver driver;

		public TemperatureSensor(IZWaveTemperatureDriver driver)
		{
			this.driver = driver;

			driver.OnChange = (x) =>
			{
				TemperatureChanged(x);
			};

			State = new SimpleSensorState<float>();
		}

		void TemperatureChanged(float temp)
		{
			State = new SimpleSensorState<float> { Value = temp };
			OnUpdate?.Invoke(this, State);
		}

		public Task<List<IDeviceState>> GetState()
		{
			return Task.FromResult(new List<IDeviceState> { State });
		}
	}
}
