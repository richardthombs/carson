using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Experiment1
{
	public interface IDevice
	{
		string Name { get; }
		string Plural { get; }
		Area Area { get; set; }
		bool IsDead { get; set; }

		Task<List<IDeviceState>> GetState();
	}
}
