namespace Experiment1
{
	public class SwitchState : IDeviceState
	{
		public bool? On { get; set; }

		public override string ToString()
		{
			if (On == null) return "unknown";

			return On.Value? "on" : "off";
		}
	}

	public class ColorState : IDeviceState
	{
		public string Color { get; set; }

		public override string ToString()
		{
			return Color ?? "unknown";
		}
	}

	public class LevelState : IDeviceState
	{
		public int? Level { get; set; }

		public override string ToString()
		{
			if (Level == null) return "unknown";
			return Level.ToString();
		}
	}
}
