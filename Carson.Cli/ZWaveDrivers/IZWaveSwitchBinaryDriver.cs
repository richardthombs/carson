using System.Threading.Tasks;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveSwitchBinaryDriver
	{
		Task Set(bool value);
		Task<bool?> Get();
	}
}
