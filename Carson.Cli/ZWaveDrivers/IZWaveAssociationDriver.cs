using System.Collections.Generic;
using System.Threading.Tasks;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveAssociationDriver
	{
		Task<Dictionary<byte, List<byte>>> Get();

	}
}
