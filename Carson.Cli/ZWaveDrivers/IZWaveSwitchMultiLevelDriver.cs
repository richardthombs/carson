using System.Threading.Tasks;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveSwitchMultiLevelDriver
	{
		Task Set(byte value);
		Task<byte?> Get();
	}
}
