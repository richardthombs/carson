using System;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveWakeUpDriver
	{
		Action<bool> OnWakeUp { get; set; }
	}
}
