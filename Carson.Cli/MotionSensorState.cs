namespace Experiment1
{
	public class MotionSensorState : IDeviceState
	{
		public bool? Detected { get; set; }

		public override string ToString()
		{
			if (Detected == null) return "unknown";

			return Detected.Value ? "motion detected" : "no motion";
		}
	}
}
