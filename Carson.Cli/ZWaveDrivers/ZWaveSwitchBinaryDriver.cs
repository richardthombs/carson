using System.Threading.Tasks;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveSwitchBinaryDriver : IZWaveSwitchBinaryDriver
	{
		Node node;
		bool? state;

		public ZWaveSwitchBinaryDriver(Node device)
		{
			this.node = device;
		}

		public async Task Set(bool value)
		{
			var binarySwitch = node.GetCommandClass<SwitchBinary>();
			await binarySwitch.Set(value);
			state = value;
		}

		public Task<bool?> Get()
		{
			return Task.FromResult(state);
		}
	}
}
