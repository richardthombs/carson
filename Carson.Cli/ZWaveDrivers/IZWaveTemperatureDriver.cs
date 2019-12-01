using System;
using System.Threading.Tasks;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveTemperatureDriver
	{
		Action<float> OnChange { get; set; }
		Task<float?> Get();
	}
}
