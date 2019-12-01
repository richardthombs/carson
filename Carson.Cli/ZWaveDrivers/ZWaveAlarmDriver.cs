using System;
using System.Threading.Tasks;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveAlarmDriver : IZWaveAlarmDriver
	{
		Node node;
		Alarm alarm;
		bool? state;

		public ZWaveAlarmDriver(Node node)
		{
			this.node = node;
			this.alarm = node.GetCommandClass<Alarm>();
			alarm.Changed += Alarm_Changed;
		}

		public Action<bool> OnChange { get; set; }

		public Task<bool?> Get()
		{
			return Task.FromResult(state);
		}

		private void Alarm_Changed(object sender, ReportEventArgs<AlarmReport> e)
		{
			if (e.Report.Detail == AlarmDetailType.MotionDetectionUnknownLocation) state = true;
			else if (e.Report.Detail == AlarmDetailType.None) state = false;

			if (state.HasValue) OnChange?.Invoke(state.Value);
		}
	}
}
