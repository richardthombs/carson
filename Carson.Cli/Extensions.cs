using System;

namespace Experiment1
{
	static class Extensions
	{
		public static bool IsOlderThan(this DateTimeOffset timestamp, TimeSpan value)
		{
			return (DateTimeOffset.UtcNow - timestamp) > value;
		}

		public static bool IsOlderThan(this DateTimeOffset? timestamp, TimeSpan value)
		{
			if (!timestamp.HasValue) return true;

			return timestamp.Value.IsOlderThan(value);
		}
	}
}
