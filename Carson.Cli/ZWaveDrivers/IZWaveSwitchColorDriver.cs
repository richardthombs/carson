using System.Threading.Tasks;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveSwitchColorDriver
	{
		Task Set(string colour);
		Task<string> Get();
	}
}
