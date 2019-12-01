using System;
using System.Threading.Tasks;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveBasicDriver : IZWaveBasicDriver
	{
		Node node;
		byte? state;

		public ZWaveBasicDriver(Node node)
		{
			this.node = node;
		}

		public async Task Set(byte value)
		{
			var sw = node.GetCommandClass<Basic>();
			await sw.Set(value);
			state = value;
		}

		public Task<byte?> Get()
		{
			return Task.FromResult(state);
		}
	}
}
