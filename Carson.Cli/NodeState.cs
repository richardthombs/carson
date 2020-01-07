using System;
using System.Collections.Generic;

using ZWave;
using ZWave.Channel;

namespace Experiment1
{
	class NodeState
	{
		public string Name;
		public string Alias;

		public DateTimeOffset? FirstFailed;
		public DateTimeOffset? LastFailed;
		public int FailCount;

		public bool HasBattery;
		public DateTimeOffset? LastWakeUp;

		public DateTimeOffset? FirstContact;
		public DateTimeOffset? LastContact;

		public List<CommandClass> CommandClasses;
		public GenericType GenericType;

		public DateTimeOffset? FirstAdded;

		public Report<BatteryReport> BatteryReport;
		public Report<SensorMultiLevelReport> TemperatureReport;
		public Report<SensorMultiLevelReport> LuminanceReport;
		public Report<SensorMultiLevelReport> RelativeHumidityReport;

		public void RecordFailure()
		{
			if (FirstFailed == null) FirstFailed = DateTimeOffset.UtcNow;
			LastFailed = DateTimeOffset.UtcNow;
			FailCount++;
		}

		void ResetFailure()
		{
			LastFailed = FirstFailed = null;
			FailCount = 0;
		}

		public void RecordContact()
		{
			if (FirstContact == null) FirstContact = DateTimeOffset.UtcNow;
			LastContact = DateTimeOffset.UtcNow;

			ResetFailure();
		}
	}
}
