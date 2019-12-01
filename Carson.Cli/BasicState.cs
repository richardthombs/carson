namespace Experiment1
{
	public class BasicState : IDeviceState
	{
		public byte? Value { get; set; }

		public override string ToString()
		{
			if (Value == null) return "unknown";

			return Value.ToString();
		}
	}
}
