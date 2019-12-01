using System;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveWakeUpDriver : IZWaveWakeUpDriver
	{
		Node node;
		WakeUp wakeUp;
		bool? state;

		public ZWaveWakeUpDriver(Node node)
		{
			this.node = node;
			this.wakeUp = node.GetCommandClass<WakeUp>();
			wakeUp.Changed += WakeUp_Changed;
		}

		public Action<bool> OnWakeUp { get; set; }

		private void WakeUp_Changed(object sender, ReportEventArgs<WakeUpReport> e)
		{
			state = e.Report.Awake;
			OnWakeUp?.Invoke(state.Value);
		}
	}
}
