namespace Experiment1
{
	public class SimpleSensorState<T> : IDeviceState where T : struct
	{
		public T? Value { get; set; }

		public override string ToString()
		{
			return Value.HasValue ? Value.Value.ToString() : "unknown";
		}
	}
}
