using System;

namespace Experiment1
{
	class Report<T>
	{
		public DateTimeOffset Timestamp;
		public T Data;
	}

	class BatteryReport
	{
		public byte Value;
		public bool IsLow;

		public override string ToString()
		{
			return IsLow ? "Low" : $"Value:{Value}%";
		}
	}
}
