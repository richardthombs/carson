using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Experiment1.ZWaveDrivers;

namespace Experiment1
{
	public class GenericDevice : IDevice
	{
		public string Name { get; set; }
		public string Plural { get; set; }
		public Area Area { get; set; }
		public bool IsDead { get; set; }

		public IZWaveBasicDriver Basic { get; set; }
		public IZWaveSwitchBinaryDriver SwitchBinary { get; set; }
		public IZWaveSwitchMultiLevelDriver SwitchMultiLevel { get; set; }
		public IZWaveSwitchColorDriver SwitchColor { get; set; }
		public IZWaveAlarmDriver Alarm { get; set; }
		public IZWaveWakeUpDriver WakeUp { get; set; }
		public IZWaveCentralSceneDriver CentralScene { get; set; }

		public Action<IDevice, MotionSensorState> OnAlarm { get; set; }

		public async Task<List<IDeviceState>> GetState()
		{
			var states = new List<IDeviceState>();
			if (Basic != null) states.Add(new BasicState { Value = await Basic.Get() });
			if (SwitchBinary != null) states.Add(new SwitchState { On = await SwitchBinary.Get() });
			if (Alarm != null) states.Add(new MotionSensorState { Detected = await Alarm.Get() });
			return states;
		}
	}
}
