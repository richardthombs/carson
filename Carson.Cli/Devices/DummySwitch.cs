using System;
using System.Threading.Tasks;

using Experiment1.ZWaveDrivers;

namespace Experiment1
{
	public class FakeSwitch : IZWaveSwitchBinaryDriver
	{
		bool? state;

		public Action<bool> OnChange { get; set; }

		public Task Set(bool value)
		{
			if (state == null || state != value)
			{
				state = value;
				OnChange?.Invoke(value);
			}
			return Task.CompletedTask;
		}

		public Task<bool?> Get()
		{
			return Task.FromResult(state);
		}
	}
}
