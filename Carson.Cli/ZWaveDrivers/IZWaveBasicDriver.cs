using System.Threading.Tasks;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveBasicDriver
	{
		Task Set(byte value);
		Task<byte?> Get();
	}
}
