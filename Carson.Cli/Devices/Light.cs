using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

using Experiment1.ZWaveDrivers;

namespace Experiment1
{
	public interface ILight : IDevice
	{
		Task SetOn(bool value);
		Task SetLevel(int value);
		Task SetColor(string value);
	}

	public class Light : ILight
	{
		public SuperNode Node { get; set; }

		public string Name { get; set; }
		public string Plural { get; set; }
		public Area Area { get; set; }
		public bool IsDead { get; set; }

		public bool? On { get; private set; }
		public int? Level { get; private set; }
		public string Color { get; private set; }

		public async Task SetOn(bool value)
		{
			if (Node.Supports(CommandClass.Basic)) await Node.Node.GetCommandClass<Basic>().Set((byte)(value ? 255 : 0));
			else if (Node.Supports(CommandClass.SwitchBinary)) await Node.Node.GetCommandClass<SwitchBinary>().Set(value);
			else if (Node.Supports(CommandClass.SwitchMultiLevel)) await Node.Node.GetCommandClass<SwitchMultiLevel>().Set((byte)(value ? 255 : 0));

//			if (basicDriver != null) await basicDriver.Set((byte)(value ? 255 : 0));
//			else if (switchBinaryDriver != null) await switchBinaryDriver.Set(value);
//			else if (switchMultiLevelDriver != null) await switchMultiLevelDriver.Set((byte)(value ? 255 : 0));
			else throw new NotSupportedException();

			On = value;
		}

		public async Task SetLevel(int value)
		{
			if (value != 255 && (value < 0 || value > 99)) throw new ArgumentOutOfRangeException();
			if (Node.Supports(CommandClass.SwitchMultiLevel)) await Node.Node.GetCommandClass<SwitchMultiLevel>().Set((byte)value);
			else throw new NotSupportedException();

			Level = value;
		}

		public async Task SetColor(string value)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			var color = ZWaveSwitchColorDriver.GetColorComponents(value);
			if (Node.Supports(CommandClass.Color)) await Node.Node.GetCommandClass<Color>().Set(color);
			else throw new NotSupportedException();

			Color = value;
		}

		public Task<List<IDeviceState>> GetState()
		{
			return Task.FromResult(new List<IDeviceState>
			{
				new SwitchState { On = On }
			});
		}
	}
}
