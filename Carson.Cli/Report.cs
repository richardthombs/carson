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

	class SensorMultiLevelReport
	{
		public ZWave.CommandClasses.SensorType Type;
		public float Value;
		public string Unit;
		public byte Scale;

		public override string ToString()
		{
			return $"Type:{Type}, Value:\"{Value} {Unit}\"";
		}
	}

	class AlarmReport
	{
		public ZWave.CommandClasses.AlarmType Type;
		public byte Level;
		public ZWave.CommandClasses.AlarmDetailType Detail;
		public byte Unknown;

		public override string ToString()
		{
			return $"Type:{Type}, Level:{Level}, Detail:{Detail}, Unknown:{Unknown}";
		}
	}

	class ManufacturerSpecificReport
	{
		public ushort ManufacturerID;
		public ushort ProductType;
		public ushort ProductID;

		public override string ToString()
		{
			return $"ManufacturerID:{ManufacturerID:X4}, ProductType:{ProductType:X4}, ProductID:{ProductID:X4}";
		}
	}
}
