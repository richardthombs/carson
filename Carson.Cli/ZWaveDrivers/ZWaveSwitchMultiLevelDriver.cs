using System;
using System.Threading.Tasks;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveSwitchMultiLevelDriver : IZWaveSwitchMultiLevelDriver
	{
		Node node;
		byte? state;

		public ZWaveSwitchMultiLevelDriver(Node node)
		{
			this.node = node;
		}

		public async Task Set(byte value)
		{
			var sw = node.GetCommandClass<SwitchMultiLevel>();
			await sw.Set(value);
			state = value;
		}

		public Task<byte?> Get()
		{
			return Task.FromResult(state);
		}
	}
}
