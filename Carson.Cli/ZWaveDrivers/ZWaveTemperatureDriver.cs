using System;
using System.Threading.Tasks;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveTemperatureDriver : IZWaveTemperatureDriver
	{
		Node node;
		float? state;
		SensorMultiLevel sensor;
		WakeUp wakeUp;

		public ZWaveTemperatureDriver(Node node)
		{
			this.node = node;
			sensor = node.GetCommandClass<SensorMultiLevel>();
			sensor.Changed += Sensor_Changed;
			wakeUp = node.GetCommandClass<WakeUp>();
			wakeUp.Changed += WakeUp_Changed;
		}

		public Action<float> OnChange { get; set; }

		public Task<float?> Get()
		{
			return Task.FromResult(state);
		}

		void Sensor_Changed(object sender, ReportEventArgs<SensorMultiLevelReport> e)
		{
			if (e.Report.Type != SensorType.Temperature) return;

			var celsius = (float)Math.Round(e.Report.Unit.Contains("F") ? (e.Report.Value - 32) / 1.8 : e.Report.Value, 1);

			UpdateState(celsius);
		}

		void WakeUp_Changed(object sender, ReportEventArgs<WakeUpReport> e)
		{
			if (e.Report.Awake)
			{
				sensor.Get(SensorType.Temperature);
			}
		}

		void UpdateState(float temp)
		{
			state = temp;
			OnChange?.Invoke(temp);
		}
	}
}
