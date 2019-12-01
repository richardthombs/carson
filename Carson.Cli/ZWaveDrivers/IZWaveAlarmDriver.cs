using System;
using System.Threading.Tasks;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveAlarmDriver
	{
		Action<bool> OnChange { get; set; }
		Task<bool?> Get();
	}
}
